using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Combat
{
    public class EventArgs<T> : EventArgs
    {
        private T eventData;


        public EventArgs(T eventData)
        {
            this.eventData = eventData;
        }


        public T EventData
        {
            get { return eventData; }
        }
    }
}
