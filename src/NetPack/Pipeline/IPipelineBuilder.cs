﻿using Microsoft.AspNetCore.Builder;
using NetPack.Pipes;
using NetPack.Requirements;
using System;

namespace NetPack.Pipeline
{
    public interface IPipelineBuilder
    {

        IApplicationBuilder ApplicationBuilder { get; set; }

        IPipelineBuilder Watch();

        IPipelineBuilder AddPipe(Action<PipelineInputBuilder> inputBuilder, IPipe pipe);

        IPipelineBuilder ServeOutputAsStaticFiles(string baseRequestPath = null);

        string BaseRequestPath { get; set; }

        IPipeLine BuildPipeLine();

        /// <summary>
        /// Adds a requirement to this pipeline. When the pipeline is initialised, all requirements are checked.
        /// This can be used to ensure that dependent services are installed etc, like node.js, or npm modules.
        /// </summary>
        /// <param name="requiement"></param>
        /// <returns></returns>
        IPipelineBuilder IncludeRequirement(IRequirement requiement);
    }
}