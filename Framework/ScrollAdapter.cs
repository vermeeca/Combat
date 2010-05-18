﻿//---------------------------------------------------------------------
// <copyright file="ScrollAdapter.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Surface.Core.Manipulations;

namespace CoreInteractionFramework
{
    /// <summary>
    /// Handles the details of scrolling content and flicking.
    /// 
    /// This class should be used for StateMachines which want to have a ScrollViewer behavior,
    /// such as ListBoxStateMachine.
    /// 
    /// Scroll values are given by HitTestDetails which are normalized.
    /// However for manipulations and inertia the values are converted into Screen Space
    /// so that flicking is consistent across controls. The values are then converted into
    /// scrollBar value space to update the scroll bar.
    /// 
    /// All updates are done through setting the ScrollBar.Value property for the appropriate scrollbar.
    /// This allows for consistent handling between the ScrollAdapter(ScrollViewer) and the ScrollBars.
    /// </summary>
    internal class ScrollAdapter
    {
        // The ScrollAdapter keeps track of the scrollbars
        // it actually uses their values to update it's own values.
        private ScrollBarStateMachine horizontalScrollBarStateMachine;
        private ScrollBarStateMachine verticalScrollBarStateMachine;

        // The top and left edge of the ScrollAdapter compared to the extent.
        private float verticalViewportStartPosition;
        private float horizontalViewportStartPosition;

        private float lastVerticalStartPostion = -1;
        private float lastHorizontalStartPosition = -1;
        
        // The amount of elastic bounce for this ScrollAdapter.
        private float verticalElasticity;
        private float horizontalElasticity;

        // The property corresponding to the ViewportChanged event.
        private bool gotViewportChange;

        public Orientation Orientation { get; set; }
        
        // These two dictionaries are used to track which
        // contacts are currently being updated and where
        // they were original placed on the scrollviewer.
        private Dictionary<int, ContactTargetEvent> currentContactInformation =
            new Dictionary<int, ContactTargetEvent>();

        private Dictionary<int, ContactTargetEvent> contactDownContactInformation =
            new Dictionary<int, ContactTargetEvent>();
        
        // Manipulation and inertia processing related fields.
        private Affine2DInertiaProcessor inertiaProcessor;
        private readonly Affine2DManipulationProcessor manipulationProcessor;
        private bool processInertia;
        private readonly Stopwatch stopwatch;

        // Running actual is used when processing
        // elastic boundaries.  The scrollbar values
        // are clamped bettwen 0 and 1, but the scrollviewer 
        // can move beyond those values based on the elasticity properties.
        private float runningActualValueX;
        private float runningActualValueY;

        // Manipulations are only updated on contact changed so we need
        // to track all of the manipulators.
        private readonly List<Manipulator> manipulations = new List<Manipulator>();
        private readonly List<Manipulator> removedManipulations = new List<Manipulator>();

        // The element that the ScrollAdapter is scrolling
        // its NumberOfPixelsInHorizontalAxis and NumberOfPixelsInVerticalAxis
        // properties are needed to do flicking correctly.
        private readonly UIElementStateMachine elementToScroll;

        // maximum elastic padding threshold in pixels
        private const int MaximumElasticPaddingThreshold = 4;

        /// <summary>
        /// Creates a new ScrollViewerStateMachine.
        /// </summary>
        /// <param name="controller">The UIController which dispatches hit testing.</param>
        /// <param name="elementToScroll">The element which is scrolling.</param>
        public ScrollAdapter(UIController controller, UIElementStateMachine elementToScroll)
        {
            // By default scrolling should be allowed in both directions.
            Orientation = Orientation.Both;

            this.elementToScroll = elementToScroll;

            // Handle the ScrollBars for this ScrollViewer.
            horizontalScrollBarStateMachine = 
                new ScrollBarStateMachine(controller, elementToScroll.NumberOfPixelsInHorizontalAxis, 0);
            horizontalScrollBarStateMachine.Orientation = Orientation.Horizontal;

            verticalScrollBarStateMachine = 
                new ScrollBarStateMachine(controller, 0, elementToScroll.NumberOfPixelsInVerticalAxis);
            verticalScrollBarStateMachine.Orientation = Orientation.Vertical;

            // Default to 1 (full size).
            HorizontalViewportSize = 1;
            VerticalViewportSize = 1;
          
            horizontalScrollBarStateMachine.ValueChanged += OnHorizontalScrollBarStateMachineValueChanged;
            horizontalScrollBarStateMachine.ThumbChanged += OnHorizontalScrollBarStateMachineThumbChanged;
            horizontalScrollBarStateMachine.NumberOfPixelsInHorizontalAxisChanged +=
                HorizontalScrollBarStateMachineNumberOfPixelsInHorizontalAxisChanged;

            verticalScrollBarStateMachine.ValueChanged += OnVerticalScrollBarStateMachineValueChanged;
            verticalScrollBarStateMachine.ThumbChanged += OnVerticalScrollBarStateMachineThumbChanged;
            verticalScrollBarStateMachine.NumberOfPixelsInVerticalAxisChanged +=
                VerticalScrollBarStateMachineNumberOfPixelsInVerticalAxisChanged;

            // Manipulations should only cause translations, not rotations or scaling.
            manipulationProcessor =
                new Affine2DManipulationProcessor(Affine2DManipulations.TranslateX | Affine2DManipulations.TranslateY);
            manipulationProcessor.Affine2DManipulationCompleted += OnAffine2DManipulationCompleted;
            stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Because the scrollbars can be assigned the NumberOfPixelsInVerticalAxis property may not line up
        /// with the pixels in the ScrollViewer, so we line them up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void VerticalScrollBarStateMachineNumberOfPixelsInVerticalAxisChanged(object sender, EventArgs e)
        {
            elementToScroll.NumberOfPixelsInVerticalAxis = verticalScrollBarStateMachine.NumberOfPixelsInVerticalAxis;
        }

        /// <summary>
        /// Because the scrollbars can be assigned the NumberOfPixelsInHorizontalAxis property may not line up
        /// with the pixels in the ScrollViewer, so we line them up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HorizontalScrollBarStateMachineNumberOfPixelsInHorizontalAxisChanged(object sender, EventArgs e)
        {
            elementToScroll.NumberOfPixelsInHorizontalAxis = horizontalScrollBarStateMachine.NumberOfPixelsInHorizontalAxis;
        }

        /// <summary>
        /// Gets or sets the horizontal ScrollBarStateMachine.
        /// </summary>
        public ScrollBarStateMachine HorizontalScrollBarStateMachine
        {
            get { return horizontalScrollBarStateMachine; }
            set
            {
                if (value == null)
                    throw SurfaceCoreFrameworkExceptions.ArgumentNullException("HorizontalScrollBarStateMachine");

                // Enforce horizontal orientation.
                if (value.Orientation != Orientation.Horizontal)
                {
                    value.Orientation = Orientation.Horizontal;
                }

                // If the ScrollBar is being set to it self just return.
                if (value == horizontalScrollBarStateMachine)
                    return;
               

                // The ScrollBarStateMachine is changing so unhook events.
                horizontalScrollBarStateMachine.ValueChanged -= OnHorizontalScrollBarStateMachineValueChanged;
                horizontalScrollBarStateMachine.ThumbChanged -= OnHorizontalScrollBarStateMachineThumbChanged;
                horizontalScrollBarStateMachine.NumberOfPixelsInHorizontalAxisChanged -=
                    HorizontalScrollBarStateMachineNumberOfPixelsInHorizontalAxisChanged;

                // Remove the controller which will cause the ScrollBarStateMachine to loose capture of any contacts.
                horizontalScrollBarStateMachine.Controller = null;
                horizontalScrollBarStateMachine = value;

                elementToScroll.NumberOfPixelsInHorizontalAxis = horizontalScrollBarStateMachine.NumberOfPixelsInHorizontalAxis;
                horizontalScrollBarStateMachine.ValueChanged += OnHorizontalScrollBarStateMachineValueChanged;
                horizontalScrollBarStateMachine.ThumbChanged += OnHorizontalScrollBarStateMachineThumbChanged;
                horizontalScrollBarStateMachine.NumberOfPixelsInHorizontalAxisChanged +=
                    HorizontalScrollBarStateMachineNumberOfPixelsInHorizontalAxisChanged;
            }
        }

        /// <summary>
        /// Gets or sets the vertical ScrollBarStateMachine.
        /// </summary>
        public ScrollBarStateMachine VerticalScrollBarStateMachine
        {
            get { return verticalScrollBarStateMachine; }
            set
            {
                if (value == null)
                    throw SurfaceCoreFrameworkExceptions.ArgumentNullException("VerticalScrollBarStateMachine");
 
                // Enforce vertical orientation.
                if (value.Orientation != Orientation.Vertical)
                {
                    value.Orientation = Orientation.Vertical;
                }

                // If the ScrollBar is being set to it self just return.
                if (value == verticalScrollBarStateMachine)
                    return;


                // The ScrollBarStateMachine is changing so unhook events.
                verticalScrollBarStateMachine.ValueChanged -= OnVerticalScrollBarStateMachineValueChanged;
                VerticalScrollBarStateMachine.ThumbChanged -= OnVerticalScrollBarStateMachineThumbChanged;
                VerticalScrollBarStateMachine.NumberOfPixelsInVerticalAxisChanged -=
                    VerticalScrollBarStateMachineNumberOfPixelsInVerticalAxisChanged;

                // Remove the controller which will cause the ScrollBarStateMachine to loose capture of any contacts.
                verticalScrollBarStateMachine.Controller = null;
                verticalScrollBarStateMachine = value;

                elementToScroll.NumberOfPixelsInVerticalAxis = verticalScrollBarStateMachine.NumberOfPixelsInVerticalAxis;
                verticalScrollBarStateMachine.ValueChanged += OnVerticalScrollBarStateMachineValueChanged;
                VerticalScrollBarStateMachine.ThumbChanged += OnVerticalScrollBarStateMachineThumbChanged;
                VerticalScrollBarStateMachine.NumberOfPixelsInVerticalAxisChanged +=
                    VerticalScrollBarStateMachineNumberOfPixelsInVerticalAxisChanged;
            }
        }

        /// <summary>
        /// The vertical normalized height of the viewport. 
        /// </summary>
        public float VerticalViewportSize
        {
            get
            {
                return verticalScrollBarStateMachine.ViewportSize;
            }
            set
            {
                if (value < 0 || value > 1)
                    throw SurfaceCoreFrameworkExceptions.ArgumentOutOfRangeException("VerticalViewportSize");

                verticalScrollBarStateMachine.ViewportSize = value;
            }
        }

        /// <summary>
        /// The horizontal normalized width of the viewport. 
        /// </summary>
        public float HorizontalViewportSize
        {
            get
            {
                return horizontalScrollBarStateMachine.ViewportSize;
            }
            set
            {
                if (value < 0 || value > 1)
                    throw SurfaceCoreFrameworkExceptions.ArgumentOutOfRangeException("HorizontalViewportSize");

                horizontalScrollBarStateMachine.ViewportSize = value;
            }
        }

        /// <summary>
        /// The starting normalized vertical coordinate of the viewport.
        /// </summary>
        public float VerticalViewportStartPosition
        {
            get
            {
                return verticalViewportStartPosition;
            }
            set
            {
                if (value < 0 || value > (float)(1 - VerticalViewportSize))
                    throw SurfaceCoreFrameworkExceptions.ArgumentOutOfRangeException("VerticalViewportStartPosition");

                // The start position of the viewport is controlled by the scrollbar value
                // so we need to convert to value space and clamp.
                float scrollbarValue = ConvertFromVerticalControlToValueSpace(value);

                scrollbarValue = FlickUtilities.Clamp(scrollbarValue, 0, 1);

                verticalScrollBarStateMachine.Value = scrollbarValue;
            }
        }

        /// <summary>
        /// The starting normalized horizontal coordinate of the viewport.
        /// </summary>
        public float HorizontalViewportStartPosition
        {
            get
            {
                return horizontalViewportStartPosition;
            }
            set
            {
                if (value < 0 || value > (float)( 1 - HorizontalViewportSize))
                    throw SurfaceCoreFrameworkExceptions.ArgumentOutOfRangeException("HorizontalViewportStartPosition");

                // The start position of the viewport is controlled by the scrollbar value
                // so we need to convert to value space and clamp.
                float scrollbarValue = ConvertFromHorizontalControlToValueSpace(value);

                scrollbarValue = FlickUtilities.Clamp(scrollbarValue, 0, 1);

                horizontalScrollBarStateMachine.Value = scrollbarValue;
            }
        }

        /// <summary>
        /// The left elastic margin.
        /// </summary>
        public float VerticalElasticity
        {
            get { return verticalElasticity; }
            set
            {
                if (value < 0 || value > 1)
                    throw SurfaceCoreFrameworkExceptions.ArgumentOutOfRangeException("VerticalElasticity");

                verticalElasticity = value;
            }
        }

        /// <summary>
        /// The right elastic margin.
        /// </summary>
        public float HorizontalElasticity
        {
            get { return horizontalElasticity; }
            set
            {
                if (value < 0 || value > 1)
                    throw SurfaceCoreFrameworkExceptions.ArgumentOutOfRangeException("HorizontalElasticity");

                horizontalElasticity = value;
            }
        }

        /// <summary>
        /// Gets the value of the ViewportChanged event.
        /// True if any of the viewport properties have changed since the controller was updated, false otherwise.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
                                                         "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Viewport")]
        public bool GotViewportChange
        {
            get { return gotViewportChange; }
        }

        /// <summary>
        /// Gets a value which determines if scrolling is occuring.
        /// True if scrolling, false otherwise.
        /// </summary>
        public bool IsScrolling
        {
            get
            {
                return horizontalScrollBarStateMachine.IsScrolling || verticalScrollBarStateMachine.IsScrolling;
            }
        }


        /// <summary>
        /// Fired when any of the viewport properties change.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", 
                                                         "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Viewport")]
        public event EventHandler ViewportChanged;

        /// <summary>
        /// Called when any of the viewport properties change.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
                                                         "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Viewport")]
        protected virtual void OnViewportChanged()
        {
            if (horizontalViewportStartPosition == lastHorizontalStartPosition
                && verticalViewportStartPosition == lastVerticalStartPostion)
            {
                return;
            }

            lastHorizontalStartPosition = horizontalViewportStartPosition;
            lastVerticalStartPostion = verticalViewportStartPosition;

            gotViewportChange = true;

            // Create a temp version of the delegate so that if it becomes null
            // it won't cause an exception in this handler.
            EventHandler viewportChanged = ViewportChanged;
            if (viewportChanged != null)
            {
                viewportChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Causes the viewport to scroll one page towards the top.
        /// </summary>
        public void PageUp()
        {
            verticalScrollBarStateMachine.PageBack();
        }

        /// <summary>
        /// Causes the viewport to scroll one page towards the bottom.
        /// </summary>
        public void PageDown()
        {
            verticalScrollBarStateMachine.PageForward();
        }

        /// <summary>
        /// Causes the viewport to scroll one page towards the left.
        /// </summary>
        public void PageLeft()
        {
            horizontalScrollBarStateMachine.PageBack();
        }

        /// <summary>
        /// Causes the viewport to scroll one page towards the right.
        /// </summary>
        public void PageRight()
        {
            horizontalScrollBarStateMachine.PageForward();
        }

        /// <summary>
        /// Causes the viewport to scroll the top left edge to the specifed position. 
        /// </summary>
        /// <param name="viewportTopPosition">The coordinate in y axis to scroll to.</param>
        /// <param name="viewportLeftPosition">The coordinate in x axis to scroll to.</param>
        public void ScrollTo(float viewportTopPosition, float viewportLeftPosition)
        {
            if (viewportTopPosition < 0 || viewportTopPosition > 1)
                throw SurfaceCoreFrameworkExceptions.ArgumentOutOfRangeException("viewportTopPosition");

            if (viewportLeftPosition < 0 || viewportLeftPosition > 1)
                throw SurfaceCoreFrameworkExceptions.ArgumentOutOfRangeException("viewportLeftPosition");

            horizontalScrollBarStateMachine.ScrollTo(viewportLeftPosition);
            verticalScrollBarStateMachine.ScrollTo(viewportTopPosition);
        }

        /// <summary>
        /// Causes scrolling to stop if it is currently being flicked.
        /// </summary>
        public void StopFlick()
        {
            if (processInertia)
            {
                inertiaProcessor.Complete();
            }

            horizontalScrollBarStateMachine.StopFlick();
            verticalScrollBarStateMachine.StopFlick();
        }

        /// <summary>
        /// Handle changes to the vertical ScrollBarStateMachine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVerticalScrollBarStateMachineThumbChanged(object sender, EventArgs e)
        {
            OnViewportChanged();
        }

        /// <summary>
        /// Handles changes to the horizontal ScrollBarStateMachine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHorizontalScrollBarStateMachineThumbChanged(object sender, EventArgs e)
        {
            OnViewportChanged();
        }

        /// <summary>
        /// Handle changes to the vertical ScrollBarStateMachine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVerticalScrollBarStateMachineValueChanged(object sender, EventArgs e)
        {
            if ((Orientation & Orientation.Vertical) != 0)
            {
                // Content is render based on the value and so we need to calculate the startPosition from the value and viewport size.
                verticalViewportStartPosition = ConvertFromVerticalValueToControlSpace(verticalScrollBarStateMachine.Value);
                OnViewportChanged();
            }
        }

        /// <summary>
        /// Handle changes to the horizontal ScrollBarStateMachine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHorizontalScrollBarStateMachineValueChanged(object sender, EventArgs e)
        {
            if ((Orientation & Orientation.Horizontal) != 0)
            {
                // Content is render based on the value.
                // So we need to calculate the startPosition from the value and viewport size.
                horizontalViewportStartPosition =
                    ConvertFromHorizontalValueToControlSpace(horizontalScrollBarStateMachine.Value);
                OnViewportChanged();
            }
        }

        /// <summary>
        /// Start the inertia processor in Surface screen space.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAffine2DManipulationCompleted(object sender, Affine2DOperationCompletedEventArgs e)
        {
            if (inertiaProcessor != null)
            {
                inertiaProcessor.Affine2DInertiaDelta -= OnAffine2DInertiaDelta;
                inertiaProcessor.Affine2DInertiaCompleted -= OnAffine2DInertiaCompleted;
                inertiaProcessor.Complete();
            }

            VectorF translation = new VectorF(e.TotalTranslationX, e.TotalTranslationY);

            // Don't start inertia if the tranlation is less than roughly 1/8" regardless of velocity.
            if (translation.Length < 96 / 8f)
            {
                return;
            }

            // Scroll in the opposite direction because a ScrollViewer moves with the contact.
            VectorF initialVelocity = new VectorF(-e.VelocityX, -e.VelocityY);

            // Check velocity.
            if (initialVelocity.Length < FlickUtilities.MinimumFlickVelocity)
            {
                // If velocity is below this threshold ignore it.
                return;
            }
            
            if (initialVelocity.Length >= FlickUtilities.MaximumFlickVelocity)
            {
                // If velocity is too large, reduce it to a reasonable value.
                initialVelocity.Normalize();
                initialVelocity = FlickUtilities.MaximumFlickVelocity * initialVelocity;
            }

            processInertia = true;
            inertiaProcessor = new Affine2DInertiaProcessor();

            inertiaProcessor.Affine2DInertiaDelta += OnAffine2DInertiaDelta;
            inertiaProcessor.Affine2DInertiaCompleted += OnAffine2DInertiaCompleted;
            inertiaProcessor.DesiredDisplacement = ConvertFromVerticalValueToScreenSpace(VerticalViewportSize);
            inertiaProcessor.InitialVelocityX = initialVelocity.X * HorizontalViewportSize;
            inertiaProcessor.InitialVelocityY = initialVelocity.Y * VerticalViewportSize;
            inertiaProcessor.InitialTimestamp = stopwatch.ElapsedTicks;

            inertiaProcessor.InitialOriginX = ConvertFromHorizontalValueToScreenSpace(horizontalScrollBarStateMachine.Value);
            inertiaProcessor.InitialOriginY = ConvertFromVerticalValueToScreenSpace(verticalScrollBarStateMachine.Value);
        }


        /// <summary>
        /// Inertia is complete so stop processing for it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAffine2DInertiaCompleted(object sender, Affine2DOperationCompletedEventArgs e)
        {
            processInertia = false;
        }

        /// <summary>
        /// Update the changes to inertia processing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAffine2DInertiaDelta(object sender, Affine2DOperationDeltaEventArgs e)
        {
            if ((Orientation & Orientation.Horizontal) != 0)
            {
                // Inertia and Manipulations are both processed in ScreenSpace so we need to convert to value space and updated.
                float valueX = horizontalScrollBarStateMachine.Value + ConvertFromHorizontalScreenToValueSpace(e.DeltaX);

                // Clamp X
                valueX = FlickUtilities.Clamp(valueX, 0, 1);

                horizontalScrollBarStateMachine.Value = valueX;
            }

            if ((Orientation & Orientation.Vertical) != 0)
            {
                float valueY = verticalScrollBarStateMachine.Value + ConvertFromVerticalScreenToValueSpace(e.DeltaY);

                // Clamp Y
                valueY = FlickUtilities.Clamp(valueY, 0, 1);

                // Update the scrollbars which will update the scroll viewer.

                verticalScrollBarStateMachine.Value = valueY;
            }
        }

        /// <summary>
        /// Handles the ResetState event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ProcessResetState(object sender, EventArgs e)
        {
            gotViewportChange = false;

            if (processInertia)
            {
                inertiaProcessor.Process(stopwatch.ElapsedTicks);
            }
        }

        /// <summary>
        /// Handles the ContactDown event.
        /// </summary>
        /// <param name="contactEvent"></param>
        public void ProcessContactDown(ContactTargetEvent contactEvent)
        {
            // Stop inertia if it is occuring.
            if (processInertia)
            {
                StopFlick();
            }

            // Reset the running actual to the current value.
            if (contactDownContactInformation.Count == 0)
            {
                runningActualValueY = ConvertFromVerticalValueToControlSpace(verticalScrollBarStateMachine.Value);
                runningActualValueX = ConvertFromHorizontalValueToControlSpace(horizontalScrollBarStateMachine.Value);
            }

            // There was a contact hit tested to this so added it to the manipulation processor list.
            contactDownContactInformation.Add(contactEvent.Contact.Id, contactEvent);

            // There was a contact hit tested to this so added it to the manipulation processor list.
            currentContactInformation.Add(contactEvent.Contact.Id, contactEvent);
        }


        /// <summary>
        /// Handles the ContactUp event.
        /// </summary>
        /// <param name="contactEvent"></param>
        public void ProcessContactUp(ContactTargetEvent contactEvent)
        {
            if (!currentContactInformation.ContainsKey(contactEvent.Contact.Id)) { return; }

            Manipulator? removed = null;

            currentContactInformation.Remove(contactEvent.Contact.Id);
            contactDownContactInformation.Remove(contactEvent.Contact.Id);

            // Find the manipulator that is being removed.
            for (int i = 0; i < manipulations.Count; i++)
            {
                Manipulator manipulator = manipulations[i];

                if (manipulator.ManipulatorId == contactEvent.Contact.Id)
                {
                    manipulations.Remove(manipulator);
                    removed = manipulator;
                    break;
                }
            }

            if (removed != null)
            {
                removedManipulations.Add(removed.Value);
            }

            if (contactDownContactInformation.Count == 0)
            {
                runningActualValueY = ConvertFromVerticalValueToControlSpace(verticalScrollBarStateMachine.Value);
                runningActualValueX = ConvertFromHorizontalValueToControlSpace(horizontalScrollBarStateMachine.Value);

                verticalViewportStartPosition = runningActualValueY;
                horizontalViewportStartPosition = runningActualValueX;

                OnViewportChanged();
            }
        
        }

        /// <summary>
        /// Handles the ContactChanged event.
        /// </summary>
        /// <param name="contactEvent"></param>
        public void ProcessContactChanged(ContactTargetEvent contactEvent)
        {
            // For each contact which changed update the item in the manipulation processor list.
            if (!currentContactInformation.ContainsKey(contactEvent.Contact.Id))
            {
                return;
            }

            ScrollViewerHitTestDetails hitTestDetails = contactEvent.HitTestDetails as ScrollViewerHitTestDetails;
            if (hitTestDetails == null)
            {
                return;
            }

            // Change the value for this contact.
            currentContactInformation[contactEvent.Contact.Id] = contactEvent;

            float averageX, averageY;
            float actualValueX = 0, constrainedValueX = 0;
            float actualValueY = 0, constrainedValueY = 0;

            AverageContactPosition(contactEvent, hitTestDetails, out averageX, out averageY);

            // Put the new position in the down so that we can use it to calculate change next time.
            contactDownContactInformation[contactEvent.Contact.Id] = contactEvent;

            // Transform the value in to start position space and add the change for X
            actualValueX = averageX + ConvertFromHorizontalValueToControlSpace(horizontalScrollBarStateMachine.Value);

            // Constrain X
            constrainedValueX = FlickUtilities.Clamp(actualValueX, 0, 1 - HorizontalViewportSize);

            // Transform the value into start position space and add the change for Y
            actualValueY = averageY + ConvertFromVerticalValueToControlSpace(verticalScrollBarStateMachine.Value);

            // Constrain Y
            constrainedValueY = FlickUtilities.Clamp(actualValueY, 0, 1 - VerticalViewportSize);

            // Determine if the viewport start position is outside of the 0 to 1 range.
            float constrainedRunningY = FlickUtilities.Clamp(runningActualValueY, 0, 1 - VerticalViewportSize);

            float constrainedRunningX = FlickUtilities.Clamp(runningActualValueX, 0, 1 - HorizontalViewportSize);

            if ((Orientation & Orientation.Horizontal) != 0)
            {
                // Deal with Elasticity behavior for horizontal
                if (runningActualValueX == constrainedRunningX && actualValueX == constrainedValueX)
                {
                    ClampScrollBarValueX(actualValueX, constrainedValueX);
                }
                else
                {
                    SetElasticScrollBarValueX(averageX);
                }
            }

            if ((Orientation & Orientation.Vertical) != 0)
            {
                // Deal with Elasticity behavor for vertical
                if (runningActualValueY == constrainedRunningY && actualValueY == constrainedValueY)
                {
                    ClampScrollBarValueY(actualValueY, constrainedValueY);
                }
                else
                {
                    SetElasticScrollBarValueY(averageY);
                }
            }

            // Use this contact event to update the current manipulators.
            UpdateCurrentManipulators(contactEvent, hitTestDetails);
        }

        /// <summary>
        /// Process the manipulations.
        /// </summary>
        public void ProcessManipulators()
        {
            // Update manipulation processor in Surface screen space.
            if (manipulations.Count != 0 || removedManipulations.Count != 0)
            {
                manipulationProcessor.ProcessManipulators(manipulations, removedManipulations);

                // we can clear the removed manipulators now as we don't need them anymore.
                removedManipulations.Clear();
            }
        }

        /// <summary>
        /// Averages the position of the contactEvent.HitTestDetails position based on the number of contacts.
        /// </summary>
        /// <param name="contactEvent">The new ContactTargetEvent</param>
        /// <param name="hitTestDetails">The contacEvent.HitTestDetails as ScrollViewerHitTestDetails</param>
        /// <param name="averageX"></param>
        /// <param name="averageY"></param>
        private void AverageContactPosition(ContactTargetEvent contactEvent,
                                            ScrollViewerHitTestDetails hitTestDetails,
                                            out float averageX, out float averageY)
        {
            // Get the down hitTestDetails for this contact.
            ScrollViewerHitTestDetails downDetails =
                contactDownContactInformation[contactEvent.Contact.Id].HitTestDetails as ScrollViewerHitTestDetails;

            // Calculate how much has changed since last move.
            averageX = (downDetails.HorizontalPosition - hitTestDetails.HorizontalPosition) * HorizontalViewportSize;
            averageY = (downDetails.VerticalPosition - hitTestDetails.VerticalPosition) * VerticalViewportSize;

            // Average the change by the number of contacts on the Surface.
            averageX /= contactDownContactInformation.Count;
            averageY /= contactDownContactInformation.Count;
        }

        /// <summary>
        /// Updates manipulators to reflect the contact change.
        /// </summary>
        /// <param name="contactEvent">The new ContactTargetEvent</param>
        /// <param name="details">The contacEvent.HitTestDetails as ScrollViewerHitTestDetails</param>
        private void UpdateCurrentManipulators(ContactTargetEvent contactEvent, ScrollViewerHitTestDetails details)
        {
            // Try to find the changed contact in the manipulators list.
            bool foundManipulator = false;
            for (int i = 0; i < manipulations.Count; i++)
            {
                Manipulator manipulator = manipulations[i];

                if (manipulator.ManipulatorId == contactEvent.Contact.Id)
                {
                    manipulations.Remove(manipulator);

                    Manipulator manipulatorToAdd = new Manipulator(contactEvent.Contact.Id,
                                                                   ConvertFromHorizontalValueToScreenSpace(details.HorizontalPosition),
                                                                   ConvertFromVerticalValueToScreenSpace(details.VerticalPosition));

                    // Performance: It doesn't matter where we insert, but if all the contacts are being updated, 
                    // then putting the most recent change at the end will mean that there is one less contact
                    // to go through the next time this loop is executed.
                    if (manipulations.Count == 0)
                    {
                        manipulations.Add(manipulatorToAdd);
                    }
                    else
                    {
                        manipulations.Insert(manipulations.Count - 1, manipulatorToAdd);
                    }

                    foundManipulator = true;
                    break;
                }
            }

            // The manipulator isn't in the list so add it.
            if (!foundManipulator)
            {
                manipulations.Add(new Manipulator(contactEvent.Contact.Id,
                                                  ConvertFromHorizontalValueToScreenSpace(details.HorizontalPosition),
                                                  ConvertFromVerticalValueToScreenSpace(details.VerticalPosition)));
            }
        }

        /// <summary>
        /// Sets the verticalScrollBarStateMachine.Value to what is passed in applying the elastic effect.
        /// </summary>
        /// <param name="averageY"></param>
        private void SetElasticScrollBarValueY(float averageY)
        {
            float offset = averageY;
            float maxElasticPadding = verticalElasticity;
            float result = 0;

            if (maxElasticPadding != 0.0f)
            {
                result = (float)Math.Round((1.0 - 1.0 / Math.Pow(2.0, offset / maxElasticPadding)) * maxElasticPadding, 4);
            }

            float verticalViewportValue = FlickUtilities.Clamp( averageY + runningActualValueY - result,
                                                                0f - verticalScrollBarStateMachine.ViewportSize * verticalElasticity, 
                                                                1 - VerticalViewportSize + verticalScrollBarStateMachine.ViewportSize * verticalElasticity);

            verticalViewportStartPosition = verticalViewportValue;
            runningActualValueY = verticalViewportValue;

            OnViewportChanged();
        }

        /// <summary>
        /// Sets the horizontalScrollBarStateMachine.Value to what is passed in applying the elastic effect.
        /// </summary>
        /// <param name="averageX"></param>
        private void SetElasticScrollBarValueX(float averageX)
        {
            float offset = averageX;
            float maxElasticPadding = horizontalElasticity;
            float result = 0.0f;

            if (maxElasticPadding != 0.0f)
            {
                result = (float)Math.Round((1.0 - 1.0 / Math.Pow(2.0, offset / maxElasticPadding)) * maxElasticPadding, 4);
            }

            float horizontalViewportValue = 
                FlickUtilities.Clamp(averageX + runningActualValueX - result, 
                                     0f - horizontalScrollBarStateMachine.ViewportSize * horizontalElasticity, 
                                     1 - HorizontalViewportSize + horizontalScrollBarStateMachine.ViewportSize * horizontalElasticity);

            horizontalViewportStartPosition = horizontalViewportValue;
            runningActualValueX = horizontalViewportValue;

            OnViewportChanged();
        }

        /// <summary>
        /// Sets the verticalScrollBarStateMachine.Value between 1 and 0
        /// </summary>
        /// <param name="actualValueY"></param>
        /// <param name="constrainedValueY"></param>
        private void ClampScrollBarValueY(float actualValueY, float constrainedValueY)
        {
            float scrollbarValue = ConvertFromVerticalControlToValueSpace(constrainedValueY);

            scrollbarValue = FlickUtilities.Clamp(scrollbarValue, 0f, 1f);

            verticalScrollBarStateMachine.Value = scrollbarValue;
            runningActualValueY = actualValueY;
        }

        /// <summary>
        /// Sets the horizontalScrollBarStateMachine.Value between 1 and 0
        /// </summary>
        /// <param name="actualValueX"></param>
        /// <param name="constrainedValueX"></param>
        private void ClampScrollBarValueX(float actualValueX, float constrainedValueX)
        {
            float scrollbarValue = ConvertFromHorizontalControlToValueSpace(constrainedValueX);

            scrollbarValue = FlickUtilities.Clamp(scrollbarValue, 0f, 1f);

            horizontalScrollBarStateMachine.Value = scrollbarValue;
            runningActualValueX = actualValueX;
        }

        /// <summary>
        /// Converts from vertical control space to vertical Value space.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private float ConvertFromVerticalControlToValueSpace(float value)
        {
            if (VerticalViewportSize == 1)
            {
                return 0.0f;
            }
            else
            {
                return value / (float)(1 - VerticalViewportSize);
            }
        }
 
        /// <summary>
        /// Converts from horizontal control space to horizontal Value space.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private float ConvertFromHorizontalControlToValueSpace(float value)
        {
            if (HorizontalViewportSize == 1)
            {
                return 0.0f;
            }
            else
            {
                return value / (float)(1 - HorizontalViewportSize);
            }
        }

        /// <summary>
        /// Converts from vertical Value space to vertical control space.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private float ConvertFromVerticalValueToControlSpace(float value)
        {
            return value * (float)(1 - VerticalViewportSize);
        }

        /// <summary>
        /// Converts from vertical Value space to vertical control space.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private float ConvertFromHorizontalValueToControlSpace(float value)
        {
            return value * (float)(1 - HorizontalViewportSize);
        }


        /// <summary>
        /// Converts from horizontal value space to horizontal screen space.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private float ConvertFromHorizontalValueToScreenSpace(float value)
        {
            return elementToScroll.NumberOfPixelsInHorizontalAxis * value;
        }

        /// <summary>
        /// Converts from horizontal screen space to horizontal value space.
        /// </summary>
        /// <param name="screenValue"></param>
        /// <returns></returns>
        private float ConvertFromHorizontalScreenToValueSpace(float screenValue)
        {
            if (elementToScroll.NumberOfPixelsInHorizontalAxis != 0)
            {
                return screenValue / elementToScroll.NumberOfPixelsInHorizontalAxis;
            }
            else
            {
                return 0.0f;
            }
        }

        /// <summary>
        /// Converts from vertical value space to vertical screen space.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private float ConvertFromVerticalValueToScreenSpace(float value)
        {
            return elementToScroll.NumberOfPixelsInVerticalAxis * value;
        }

        /// <summary>
        /// Converts from vertical screen space to vertical value space.
        /// </summary>
        /// <param name="screenValue"></param>
        /// <returns></returns>
        private float ConvertFromVerticalScreenToValueSpace(float screenValue)
        {
            if (elementToScroll.NumberOfPixelsInVerticalAxis != 0)
            {
                return screenValue / elementToScroll.NumberOfPixelsInVerticalAxis;
            }
            else
            {
                return 0.0f;
            }
        }

        
    }
}
