using System;
using System.Timers;

namespace MirrorEdit.Util
{
    internal class TimedRunner
    {
        private double timerDelay;
        private Timer timer;
        private Action action;

        public TimedRunner(double delay, Action runAction)
        {
            timerDelay = delay;
            action = runAction;
        }

        /// <summary>
        /// Resets the timer
        /// </summary>
        public void ResetInterval()
        {
            timer = new Timer(timerDelay);
            timer.AutoReset = true;
            timer.Elapsed += OnTimerCompleted;
            timer.Start();
        }

        private void OnTimerCompleted(object sender, ElapsedEventArgs e)
        {
            action.Invoke();
        }
    }
}