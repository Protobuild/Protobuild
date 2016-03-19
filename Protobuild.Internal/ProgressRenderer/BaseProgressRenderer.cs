using System;

namespace Protobuild
{
    internal abstract class BaseProgressRenderer
    {
        private readonly DateTime m_StartTime;
        private bool m_IsShowingProgress;
        private bool m_IsSuspended;
        private DateTime m_LastSuspensionTime;
        private bool m_DidOutput;

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

        protected void Output(string str)
        {
            Console.Write("\r" + str);
            this.m_DidOutput = true;
        }

        public void FinalizeRendering()
        {
            if (this.m_DidOutput)
            {
                Console.WriteLine();
            }
        }
    }
}

