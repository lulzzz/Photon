﻿using System;
using Photon.Framework.Server;
using Photon.Library.TcpMessages;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Server.Internal.Sessions
{
    internal class DomainAgentBuildSessionHost : DomainAgentSessionHostBase
    {
        private readonly ServerBuildSession session;


        public DomainAgentBuildSessionHost(ServerBuildSession session, ServerAgent agent, CancellationToken token) : base(session, agent, token)
        {
            this.session = session;
        }

        protected override async Task OnBeginSession(CancellationToken token)
        {
            var message = new BuildSessionBeginRequest {
                ServerSessionId = session.SessionId,
                SessionClientId = SessionClientId,
                Project = session.Project,
                Agent = Agent,
                AssemblyFile = session.AssemblyFilename,
                PreBuild = session.PreBuild,
                TaskName = session.TaskName,
                GitRefspec = session.GitRefspec,
                BuildNumber = session.Build.Number,
                Variables = session.Variables,
                Commit = session.Commit,
            };

            var response = await MessageClient.Send(message)
                .GetResponseAsync<BuildSessionBeginResponse>(token);

            AgentSessionId = response.AgentSessionId;
        }

        protected override async Task OnReleaseSessionAsync(CancellationToken token)
        {
            var message = new SessionReleaseRequest {
                AgentSessionId = AgentSessionId,
            };

            MessageClient.SendOneWay(message);

            await Task.Delay(800, token);
            MessageClient.Disconnect(TimeSpan.FromSeconds(30));
        }

        protected override void OnSessionOutput(string text)
        {
            session.Output.WriteRaw(text);
        }
    }
}
