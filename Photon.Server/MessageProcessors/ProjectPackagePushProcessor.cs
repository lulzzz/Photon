﻿using Photon.Communication;
using Photon.Communication.Messages;
using Photon.Framework.Packages;
using Photon.Library.TcpMessages;
using Photon.Server.Internal;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Photon.Server.MessageProcessors
{
    internal class ProjectPackagePushProcessor : MessageProcessorBase<ProjectPackagePushRequest>
    {
        public override async Task<IResponseMessage> Process(ProjectPackagePushRequest requestMessage)
        {
            try {
                if (!PhotonServer.Instance.Sessions.TryGet(requestMessage.ServerSessionId, out var session))
                    throw new Exception($"Agent Session ID '{requestMessage.ServerSessionId}' not found!");

                var metadata = await ProjectPackageTools.GetMetadataAsync(requestMessage.Filename);
                if (metadata == null) throw new ApplicationException("No metadata found!");

                await PhotonServer.Instance.ProjectPackages.Add(requestMessage.Filename);

                session.PushedProjectPackageList.Add(new PackageReference(metadata.Id, metadata.Version));
            }
            finally {
                try {File.Delete(requestMessage.Filename);}
                catch {}
            }

            return new ProjectPackagePushResponse();
        }
    }
}
