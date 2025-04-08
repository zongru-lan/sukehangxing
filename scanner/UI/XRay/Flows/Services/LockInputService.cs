using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.XRay.Common.Utilities;
using static System.Net.Mime.MediaTypeNames;

namespace UI.XRay.Flows.Services
{
    public class LockInputEventArgs : EventArgs
    {
        public bool State { get; set; }
        public LockInputEventArgs(bool state)
        {
            State = state;
        }
    }

    public class LockInputService
    {
        [DllImport("user32.dll")]
        static extern void BlockInput(bool Block);

        public static LockInputService Service { get; private set; }

        static LockInputService()
        {
            Service = new LockInputService();
        }
        protected LockInputService()
        {
        }
        
        private readonly AutoResetEvent _unblockEvent = new AutoResetEvent(false);
		private volatile bool _isInLocked = false;
		
        private readonly SmartWeakEvent<EventHandler<LockInputEventArgs>> _event = new SmartWeakEvent<EventHandler<LockInputEventArgs>>();
        public event EventHandler<LockInputEventArgs> LockInputEvent
        {
            add { _event.Add(value); }
            remove { _event.Remove(value); }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void BlockInputFunc(bool block)
        {
            if (block)
            {
                if (_isInLocked)
                {
                    return;
                }
                _isInLocked = true;
                Task.Run(() =>
                {
                    BlockInput(block);
                    _unblockEvent.WaitOne();
                    BlockInput(!block);
                });
            }
            else
            {
                if (_isInLocked)
                {
                    _unblockEvent.Set();
                    _isInLocked = false;
                }                
            }
            _event.Raise(this,new LockInputEventArgs(block));
        }
        
        public void Close()
        {
            _unblockEvent?.Close();
        }
    }
}
