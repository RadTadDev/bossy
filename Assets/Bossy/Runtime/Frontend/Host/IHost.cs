using System;

namespace Bossy.Frontend
{
    internal interface IHost
    {
        public IHostController Controller { get; }

        public SessionSpace Space { get; }
        
        public void Initialize(HostManager manager, Action<FrontendType, SessionSpace> createNewSession, SessionSpace space);
        
        public void Open();

        public void Hide();

        public void Close();
    }
}