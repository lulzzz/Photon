﻿using Photon.Framework.Agent;
using System.Threading.Tasks;

namespace Photon.Framework.Tasks
{
    public interface IBuildTask
    {
        IAgentBuildContext Context {get; set;}

        Task<TaskResult> RunAsync();
    }
}
