﻿using Dazinator.AspNet.Extensions.FileProviders;
using Dazinator.AspNet.Extensions.FileProviders.Directory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using NetPack.Requirements;
using NetPack.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetPack.Pipeline
{

    public class Pipeline : IPipeLine
    {

        public static TimeSpan DefaultInitialiseTimeout = new TimeSpan(0, 5, 0);
        private readonly Predicate<IPipeLine> _shouldPerformRequirementsCheck;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="environmentFileProvider">The file provider from the environment that allows this pipe to get it's files files from the environment for processing.</param>
        /// <param name="pipes">The processors in this pipeline.</param>
        /// <param name="requirements">Requirements that this pipeline needs to satisfy in order to process.</param>
        /// <param name="sourcesOutputDirectory">A directory in which source files can be output without triggering any further processing. This will be integrated with webroot file provider so that source files can be made available to the browser.</param>
        /// <param name="baseRequestPath">The base request path upon which any source files, or generated output will be resolved / served on via the browser.</param>
        /// <param name="directory">A directory in which any generated outputs will be placed. If null specified, then this pipeline will use its own instance. Allows multiple pipelines to share the same output directory which means an output generated by one pipeline could trigger processing in a seperate pipeline sharing the same output directory.</param>
        public Pipeline(
            IFileProvider environmentFileProvider,
            List<PipeProcessor> pipes,
            List<IRequirement> requirements,
            IDirectory sourcesOutputDirectory,
            ILogger<Pipeline> logger,
            string baseRequestPath = null,
            IDirectory generatedOutputDirectory = null, 
            Predicate<IPipeLine> shouldPerformRequirementsCheck = null)
        {           
            EnvironmentFileProvider = environmentFileProvider;
            Pipes = pipes;
            Requirements = requirements;            
            Logger = logger;
            _shouldPerformRequirementsCheck = shouldPerformRequirementsCheck;
            generatedOutputDirectory = generatedOutputDirectory ?? new InMemoryDirectory();
            GeneratedOutputFileProvider = new InMemoryFileProvider(generatedOutputDirectory);

            sourcesOutputDirectory = sourcesOutputDirectory ?? new InMemoryDirectory();           
            SourcesFileProvider = new InMemoryFileProvider(sourcesOutputDirectory);

            // let in memory generated netpack files override files from environment..
            // i.e if you have a physical js file on disc and a new one is generated in memory by netpack processing
            // then netpack's will take precedence.
            var generatedAndEnvironmentFileProvider = new CompositeFileProvider(GeneratedOutputFileProvider, EnvironmentFileProvider);
            WebrootFileProvider = new CompositeFileProvider(GeneratedOutputFileProvider, SourcesFileProvider);

            Context = new PipelineContext(
                generatedAndEnvironmentFileProvider,
                sourcesOutputDirectory, generatedOutputDirectory, baseRequestPath);
            // Name = Guid.NewGuid().ToString();
        }

        public PipelineContext Context { get; set; }      

        /// <summary>
        /// Provides access to files that need to be processed from the environment. 
        /// This does not include access to new files that are produced only as a result of pipeline processing.
        /// </summary>
        public IFileProvider EnvironmentFileProvider { get; set; }

        /// <summary>
        /// Provides access too all generated output files only. These are files that were output as a result of file processing some input files.
        /// </summary>
        public IFileProvider GeneratedOutputFileProvider { get; set; }

        /// <summary>
        /// Provides access to the subset of input / source files that need to be exposed / served up to the borwser, usually the case if sourcemaps are in play without inline sources.
        /// </summary>
        public IFileProvider SourcesFileProvider { get; set; }

        public ILogger<Pipeline> Logger { get; }     

        /// <summary>
        /// Provides access to all files that should be visible to webroot - i.e a browser.
        /// </summary>
        public IFileProvider WebrootFileProvider { get; set; }    

        /// <summary>
        /// The pipes in this pipeline.
        /// </summary>
        public List<PipeProcessor> Pipes { get; set; }

        /// <summary>
        /// Any requirements that should be met for this pipeline to function.
        /// </summary>
        public List<IRequirement> Requirements { get; set; }      

        public void Initialise()
        {
            // run checks for requirements.
            bool shouldCheckRequirements = _shouldPerformRequirementsCheck?.Invoke(this) ?? true;
            if(shouldCheckRequirements)
            {
                CheckRequirements();
            }
            else
            {
                Logger.LogWarning("Skipped checking requirements for pipeline - you should ensure all npm dependencies are installed etc otherwise you may get errors at runtime.");
            }

            // Trigger the pipeline to be flushed if it hasn't already.
            // we want to block becausewe dont want the app to finish starting
            // before all assets have been processed..
            //todo: exception handling here.
            ProcessUninitialisedPipesAsync(CancellationToken.None).Wait(DefaultInitialiseTimeout);

        }

        private void CheckRequirements()
        {
            Logger.LogInformation("Checking requirements for pipeline");

            foreach (IRequirement requirement in Requirements)
            {
                requirement.Check(this);
            }
        }

        /// <summary>
        /// Processes all pipes in the pipeline.
        /// </summary>
        /// <returns></returns>
        public Task ProcessAsync(CancellationToken cancelationToken)
        {
            return ProcessPipesAsync(Pipes, cancelationToken);
        }

        public bool IsBusy => Pipes.Any(a => a.IsProcessing);

        public async Task ProcessPipesAsync(IEnumerable<PipeProcessor> pipes, CancellationToken cancellationToken)
        {          
            try
            {
                IEnumerable<Task> processTasks = pipes.Select(p=>ProcessPipe(p, cancellationToken));
                Task task = Task.WhenAll(processTasks);
                await task;
            }
            catch (Exception e)
            {
                Logger.LogError(new EventId(1001), e, "Error occurred processing pipeline");               
            }               
          
        }

        public async Task ProcessPipe(PipeProcessor pipe, CancellationToken cancellationToken)
        {
            try
            {
                await pipe.ProcessChanges(this);
            }
            catch (Exception e)
            {
                Logger.LogError(new EventId(1001), e, "Error occurred processing pipe: {name}", pipe.Pipe?.Name ?? string.Empty);
            }
        }

        public async Task ProcessUninitialisedPipesAsync(CancellationToken cancellationToken)
        {

            try
            {
                await ProcessPipesAsync(Pipes.Where(a => a.IsUninitialised()), cancellationToken);
            }
            catch (Exception e)
            {
                // retry?
                throw;
            }
        }

    }
}