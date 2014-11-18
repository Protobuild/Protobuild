using System;

namespace Protobuild
{
    public abstract class BaseProgressRenderer
    {
        private readonly DateTime m_StartTime;
        private bool m_IsShowingProgress;
        private bool m_IsSuspended;
        private DateTime m_LastSuspensionTime;

        protected BaseProgressRenderer()
        {
            this.m_StartTime = DateTime.Now;
        }

        protected bool OutputAllowed { get { return this.m_IsShowingProgress && !this.m_IsSuspended; } }

        protected void Update()
        {
            if (!this.m_IsShowingProgress)
            {
                if ((DateTime.Now - this.m_StartTime).TotalSeconds >= 3)
                {
                    this.m_IsShowingProgress = true;
                    this.m_IsSuspended = false;
                }
            }
            else if (!this.m_IsSuspended)
            {
                this.m_IsSuspended = true;
                this.m_LastSuspensionTime = DateTime.Now;
            }
            else if (this.m_IsSuspended)
            {
                if ((DateTime.Now - this.m_LastSuspensionTime).TotalSeconds >= 1)
                {
                    this.m_IsSuspended = false;
                }
            }
        }
    }
}

