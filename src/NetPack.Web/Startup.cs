﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetPack.RequireJs;

namespace NetPack.Web
{
    public class Startup
    {

        public Startup(Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
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
            services.AddNetPack((setup) =>
            {
                setup.AddPipeline(pipelineBuilder =>
                {
                    pipelineBuilder.WithHostingEnvironmentWebrootProvider()
                    // Simple processor, that compiles typescript files into js files.
                    .AddTypeScriptPipe(input =>
                    {
                        input.Include("ts/*.ts");
                    }, options =>
                    {
                        options.Target = Typescript.ScriptTarget.ES5;
                        options.Module = Typescript.ModuleKind.AMD;
                        options.InlineSources = false;
                        options.InlineSourceMap = false;
                        options.NoImplicitAny = true;
                        options.SourceMap = true;

                    })
                    // Another processor that combines multiple js files into a single "bundle" file.
                    .AddJsCombinePipe(input =>
                    {
                        input.Include("ts/*.js");
                    }, () => "bundle.js")
                    // Add a require js processor that takes all AMD format javascript files and optimises them using rjs optimiser.
                    .AddRequireJsOptimisePipe(input =>
                    {
                        input.Include("amd/*.js")
                        .Include("js/requireConfig.js");
                    }, options =>
                    {
                        options.GenerateSourceMaps = true;
                        options.Optimizer = Optimisers.none;
                        options.BaseUrl = "amd";
                        // options.
                        //  options.AppDir = "amd";
                        options.Name = "SomePage"; // The name of the AMD module to optimise.
                        options.Out = "built.js"; // The name of the output file.

                        // Here we list the module names
                        //options.Modules.Add(new ModuleInfo() { Name = "ModuleA" });
                        //options.Modules.Add(new ModuleInfo() { Name = "ModuleB" });
                        //  options.Modules.Add(new ModuleInfo() { Name = "SomePage" });
                    })
                     .AddJsMinPipe(input =>
                     {
                         input.Include("js/*.js");
                     })
                     .AddRollupPipe(input =>
                     {
                         input.Include("amd/*.js")
                              .Include("js/requireConfig.js");
                     }, options =>
                     {
                         // we add a rollup plugin which converts AMD files to ES2016 modules, so they can be included in a rollup bundle.
                         // https://www.npmjs.com/package/rollup-plugin-amd
                         options.AddPlugin((a) =>
                         {
                             a.RequiresNpmModule("module-lookup-amd", "5.0.1")
                              .ImportOnly()   // only imported into the script as default export name, won't be included in rollupjs plugins list, but other plugin config could reference it.
                              .DefaultExportName("lookup");                           
                         })
                         .AddPlugin((a) =>
                         {
                             a.RequiresNpmModule("rollup-plugin-amd", "3.0.0")
                             .Register((amdPluginOptions) =>
                             {
                                 amdPluginOptions.rewire = "FUNCfunction (moduleId, parentPath) { return lookup({ partial: moduleId, filename: parentPath, config: {baseUrl: '/amd'} }); }FUNC";
                              //   amdPluginOptions.rewire = "FUNCfunction (moduleId, parentPath) { return lookup({ partial: moduleId, filename: parentPath, config: './js/requireConfig.js' }); }FUNC";
                             });
                             // a.WithConfiguration();
                         });
                         options.InputOptions.Input = "/amd/SomePage.js";

                         options.AddOutput((output) => {
                             output.Format = Rollup.RollupOutputFormat.Iife;
                             output.File = "rollupbundle.js";                           
                         });                       
                     })
                     // rollup code splitting example.
                      .AddRollupCodeSplittingPipe(input =>
                      {
                          input.Include("esm/**/*.js");
                      }, options =>
                      {
                          options.InputOptions.AddEntryPoint("/esm/main-a.js")
                                              .AddEntryPoint("/esm/main-b.js");

                          options.AddOutput((output) => {
                              output.Format = Rollup.RollupOutputFormat.Esm;
                              output.Dir = "/rollup/module/";
                          });                        
                      })
                      // rollup code splitting example - for browsers that don't support native modules we use systemjs.
                      .AddRollupCodeSplittingPipe(input =>
                      {
                          input.Include("esm/**/*.js");
                      }, options =>
                      {
                          options.InputOptions.AddEntryPoint("/esm/main-a.js")
                                              .AddEntryPoint("/esm/main-b.js");

                          options.AddOutput((output) => {
                              output.Format = Rollup.RollupOutputFormat.System;
                              output.Dir = "/rollup/nomodule/";
                          });
                         
                      })
                      // rollup code splitting example - produces multiple rollup builds (different output formats)
                      // from same set of input files sent to nodejs side once.
                      .AddRollupCodeSplittingPipe(input =>
                      {
                          input.Include("esm/**/*.js");
                      }, options =>
                      {
                          options.InputOptions.AddEntryPoint("/esm/main-a.js")
                                              .AddEntryPoint("/esm/main-b.js");

                          options.AddOutput((output) => {
                              output.Format = Rollup.RollupOutputFormat.System;
                              output.Dir = "/rollup/multi/nomodule/";
                          });

                          options.AddOutput((output) => {
                              output.Format = Rollup.RollupOutputFormat.System;
                              output.Dir = "/rollup/multi/module/";
                          });

                      })

                    .UseBaseRequestPath("/netpack") // serves all outputs using the specified base request path.
                    .Watch(); // Inputs are monitored, and when changes occur, pipes will automatically re-process.

                });
            });

            services.AddBrowserReload((options) =>
            {
                // trigger browser reload when our bundle file changes.
                options.WatchWebRoot("/netpack/built.js");
                options.WatchContentRoot("/Views/**/*.cshtml");
            });

            services.AddMvc();
            services.AddSignalR();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseNetPack();
            app.UseStaticFiles();

            app.UseBrowserReload();

            // UseBrowserReload() calls UseSignalR() under the hood with default options.
            // If you want full control of singlar setup, use the following instead:
            //app.UseSignalR(routes =>
            //{
            //    routes.MapBrowserReloadHub();
            //});

            app.UseMvc(routes =>
             {
                 routes.MapRoute(
                     name: "default",
                     template: "{controller=Home}/{action=SingleTypescriptFile}/{id?}");
             });

            app.Use(async (context, next) =>
            {
                //  await context.Response.WriteAsync("Pre Processing");

                await next();

                //  await context.Response.WriteAsync("Post Processing");
            });           
          
        }

    }
}
