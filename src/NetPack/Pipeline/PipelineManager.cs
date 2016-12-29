using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Composite;
using Microsoft.Extensions.Options;
using Dazinator.AspNet.Extensions.FileProviders;

namespace NetPack.Pipeline
{
    /// <summary>
    /// Provides access to all pipelines.
    /// </summary>
    public class PipelineManager
    {

        private readonly IHostingEnvironment _hostingEnv;

        public PipelineManager(IHostingEnvironment env, IPipelineWatcher watcher)
        {
            _hostingEnv = env;
            Watcher = watcher;
            PipeLines = new Dictionary<string, IPipeLine>();
        }

        public IPipelineWatcher Watcher { get; set; }

        public Dictionary<string, IPipeLine> PipeLines { get; set; }

        public void AddPipeLine(string key, IPipeLine pipeline, bool watch)
        {
            PipeLines.Add(key, pipeline);
            SetupPipeline(pipeline, watch);
        }

        private void SetupPipeline(IPipeLine pipeline, bool watch)
        {
            //  var pipeLineWatcher = appBuilder.ApplicationServices.GetService<IPipelineWatcher>();
            if (watch)
            {
                Watcher.WatchPipeline(pipeline);
            }

            var outputFileProvider = pipeline.WebrootFileProvider;
            if (!string.IsNullOrWhiteSpace(pipeline.BaseRequestPath))
            {
                outputFileProvider = new RequestPathFileProvider(pipeline.BaseRequestPath, outputFileProvider);
            }


            if (_hostingEnv.WebRootFileProvider == null || _hostingEnv.WebRootFileProvider is NullFileProvider)
            {
                _hostingEnv.WebRootFileProvider = outputFileProvider;
            }
            else
            {
                var composite = new CompositeFileProvider(_hostingEnv.WebRootFileProvider, outputFileProvider);
                _hostingEnv.WebRootFileProvider = composite;
            }

            pipeline.Initialise();
        }

        IPipeLine GetPipeline(string name)
        {
            return PipeLines[name];
            // var pipeline = 
        }


    }
}