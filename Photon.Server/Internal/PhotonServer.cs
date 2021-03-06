﻿using log4net;
using Photon.Communication;
using Photon.Framework;
using Photon.Library;
using Photon.Library.HttpSecurity;
using Photon.Library.Packages;
using Photon.Library.Variables;
using Photon.Server.Internal.Projects;
using Photon.Server.Internal.ServerAgents;
using Photon.Server.Internal.ServerConfiguration;
using Photon.Server.Internal.Sessions;
using PiServerLite.Http;
using PiServerLite.Http.Content;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Server.Internal
{
    internal class PhotonServer : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PhotonServer));
        public static PhotonServer Instance {get;} = new PhotonServer();

        private HttpReceiver receiver;
        private bool isStarted;

        public ProjectManager Projects {get;}
        public ServerSessionManager Sessions {get;}
        public ScriptQueue Queue {get;}
        public ProjectPackageManager ProjectPackages {get;}
        public ApplicationPackageManager ApplicationPackages {get;}
        public MessageProcessorRegistry MessageRegistry {get;}
        public VariableSetDocumentManager Variables {get;}

        public ServerConfigurationManager ServerConfiguration {get;}
        public ServerAgentManager Agents {get;}


        public PhotonServer()
        {
            Projects = new ProjectManager();
            Sessions = new ServerSessionManager();
            MessageRegistry = new MessageProcessorRegistry();
            Variables = new VariableSetDocumentManager();

            ProjectPackages = new ProjectPackageManager {
                PackageDirectory = Configuration.ProjectPackageDirectory,
            };

            ApplicationPackages = new ApplicationPackageManager {
                PackageDirectory = Configuration.ApplicationPackageDirectory,
            };

            Queue = new ScriptQueue {
                MaxDegreeOfParallelism = Configuration.Parallelism,
            };

            ServerConfiguration = new ServerConfigurationManager();
            Agents = new ServerAgentManager();
        }

        public void Dispose()
        {
            if (isStarted) Stop();

            Queue?.Dispose();
            Sessions?.Dispose();
            receiver?.Dispose();
            receiver = null;
        }

        public void Start()
        {
            if (isStarted) throw new Exception("Server has already been started!");
            isStarted = true;

            Log.Debug("Starting Server...");

            // Load existing or default server configuration
            ServerConfiguration.Load();

            MessageRegistry.Scan(Assembly.GetExecutingAssembly());
            MessageRegistry.Scan(typeof(ILibraryAssembly).Assembly);
            MessageRegistry.Scan(typeof(IFrameworkAssembly).Assembly);

            // TODO: Cache Project Package Index?
            //ProjectPackages.Initialize();

            Sessions.Start();
            Queue.Start();

            var taskVariables = Task.Run(() => Variables.Load(Configuration.VariablesDirectory));
            var taskHttp = Task.Run(() => StartHttpServer());
            var taskAgents = Task.Run(() => Agents.Load());
            var taskProjects = Task.Run(() => Projects.Load());

            Task.WaitAll(
                taskVariables,
                taskAgents,
                taskProjects,
                taskHttp);

            Log.Info("Server started.");
        }

        public void Stop()
        {
            Log.Debug("Stopping Server...");

            Queue.Stop();
            Sessions.Stop();

            try {
                receiver?.Dispose();
                receiver = null;
            }
            catch (Exception error) {
                Log.Error("Failed to stop HTTP Receiver!", error);
            }

            Log.Info("Server stopped.");
        }

        public async Task Shutdown(TimeSpan timeout)
        {
            Log.Debug("Shutdown started...");

            using (var tokenSource = new CancellationTokenSource(timeout)) {
                var token = tokenSource.Token;

                token.Register(() => {
                    Queue.Abort();
                    Sessions.Abort();
                });

                await Task.Run(() => {
                    Queue.Stop();
                    Sessions.Stop();
                }, token);
            }
        }

        public void Abort()
        {
            Log.Debug("Server abort started...");

            Queue.Abort();
            Sessions.Abort();

            try {
                receiver?.Dispose();
                receiver = null;
            }
            catch (Exception error) {
                Log.Error("Failed to stop HTTP Receiver!", error);
            }
        }

        private void StartHttpServer()
        {
            var enableSecurity = ServerConfiguration.Value.Security?.Enabled ?? false;
            var http = ServerConfiguration.Value.Http;

            var contentDir = new ContentDirectory {
                DirectoryPath = Configuration.HttpContentDirectory,
                UrlPath = "/Content/",
            };

            var sharedContentDir = new ContentDirectory {
                DirectoryPath = Configuration.HttpSharedContentDirectory,
                UrlPath = "/SharedContent/",
            };

            var context = new HttpReceiverContext {
                ListenerPath = http.Path,
                ContentDirectories = {
                    contentDir,
                    sharedContentDir,
                },
            };

            if (enableSecurity) {
                var ldapAuth = new LdapAuthorization();

                context.SecurityMgr = new ServerHttpSecurity {
                    Authorization = ldapAuth,
                };
            }

            context.Views.AddFolderFromExternal(Configuration.HttpViewDirectory);

            var httpPrefix = $"http://{http.Host}:{http.Port}/";

            if (!string.IsNullOrEmpty(http.Path))
                httpPrefix = NetPath.Combine(httpPrefix, http.Path);

            if (!httpPrefix.EndsWith("/"))
                httpPrefix += "/";

            receiver = new HttpReceiver(context);
            receiver.Routes.Scan(Assembly.GetExecutingAssembly());
            receiver.AddPrefix(httpPrefix);

            try {
                receiver.Start();

                Log.Debug($"HTTP Server listening at {httpPrefix}");
            }
            catch (Exception error) {
                Log.Error("Failed to start HTTP Receiver!", error);
            }
        }
    }
}
