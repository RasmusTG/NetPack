﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using NetPack;
using NetPack.Pipes.Typescript;

namespace NetPack.Web
{
    public class Startup
    {

        private List<IFileProvider> _fileProviders = new List<IFileProvider>();

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
            services.AddNetPack();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

           

            var tsFileProvider = new PhysicalFileProvider(env.ContentRootPath + "\\wwwroot\\ts\\");

            CompileIndividualTypescriptFiles(app, tsFileProvider);
            CompileIndividualTypescriptFilesThenCombineThem(app, tsFileProvider);


            //_fileProviders.Add(env.WebRootFileProvider);

            // See issue: https://github.com/aspnet/StaticFiles/issues/158
           // env.WebRootFileProvider = new CompositeFileProvider(_fileProviders);

            if (!env.IsProduction())
            {
                // add another pipeline that takes outputs from previous pipeline and bundles them


            }

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=SingleTypescriptFile}/{id?}");
            });

        }

        private void CompileIndividualTypescriptFiles(IApplicationBuilder app, IFileProvider typescriptFileProvider)
        {


            var contentPipeline = app.UseFileProcessing(pipelineBuilder =>
             {
                 pipelineBuilder
                    .Take(files =>
                    {
                        files
                            .Include("Another.ts")
                            .Include("Greeter.ts");
                    }, typescriptFileProvider)
                                    .Watch()
                    .BeginPipeline()
                        .AddTypeScriptPipe(tsConfig =>
                        {
                            tsConfig.Target = TypeScriptPipeOptions.ScriptTarget.Es5;
                            tsConfig.Module = TypeScriptPipeOptions.ModuleKind.CommonJs;
                            tsConfig.NoImplicitAny = true;
                            tsConfig.RemoveComments = false;
                            tsConfig.SourceMap = true;
                        })
                     .BuildPipeLine();
             })
             .UseOutputAsStaticFiles("netpack/ts");

            // Can access useful properties of the pipeline here like the FileProvider used to serve outputs from the pipeline.
            //  var pipelineOutputsFileProvider = contentPipeline.PipelineFileProvider;
            //  var existingFileProvider = environment.WebRootFileProvider;
            // var allFiles = existingFileProvider.GetDirectoryContents("");

            _fileProviders.Add(contentPipeline.PipelineFileProvider);

        }


        private void CompileIndividualTypescriptFilesThenCombineThem(IApplicationBuilder app, IFileProvider typescriptFileProvider)
        {
            
            var contentPipeline = app.UseFileProcessing(pipelineBuilder =>
            {
                pipelineBuilder
                   .Take(files =>
                   {
                       files
                           .Include("Another.ts")
                           .Include("Greeter.ts");
                   }, typescriptFileProvider)
                                   .Watch()
                   .BeginPipeline()
                       .AddTypeScriptPipe(tsConfig =>
                       {
                           tsConfig.Target = TypeScriptPipeOptions.ScriptTarget.Es5;
                           tsConfig.Module = TypeScriptPipeOptions.ModuleKind.CommonJs;
                           tsConfig.NoImplicitAny = true;
                           tsConfig.RemoveComments = false;
                           tsConfig.SourceMap = true;

                       })
                       .AddJsCombinePipe(combineConfig =>
                       {
                           combineConfig.CombinedJsFileName = "bundleA.js";
                       })
                    .BuildPipeLine();
            })
              .UseOutputAsStaticFiles("netpack/tsbundle");

            _fileProviders.Add(contentPipeline.PipelineFileProvider);
        }




    }
}
