using System;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.External
{
    public static class SimMessaging
    {
        public static readonly ConcurrentQueue<ExampleMessage> ExampleMessageQueue = ConcurrentQueue<ExampleMessage>.Create_WillNeverBeGCed( "SimMessaging-ExampleMessageQueue" );
	}
}
