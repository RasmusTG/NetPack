﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using NetPack.Pipeline;

namespace NetPack.RequireJs
{
    public class UglifyMinifyJsPipe : IPipe
    {
        public async Task ProcessAsync(PipeContext context, CancellationToken cancelationToken)
        {
           // var pipeContext = context.PipeContext;
            // Need to run uglify on any .js, .css, .html, or .htm files passed through the pipeline.
            foreach (var inputFile in context.InputFiles)
            {
                // only interested in typescript files.
                var inputFileInfo = inputFile.FileInfo;
                MinifyJsFile(context, inputFileInfo);

                //var ext = System.IO.Path.GetExtension(inputFileInfo.Name).ToLowerInvariant();
                //switch (ext)
                //{
                //    case ".css":
                //        MinifyCssFile(context, inputFileInfo);
                //        break;
                //    case ".html":
                //    case ".htm":
                       
                //        break;
                //    case ".js":
                //        MinifyJsFile(context, inputFileInfo);
                //        break;
                //    default:
                //        // allow other file types to pass through untouched.
                //     //   context.AddOutput(inputFile);
                //        break;

                //}
            }
        }
        
        private void MinifyJsFile(PipeContext context, IFileInfo inputFileInfo)
        {
            throw new NotImplementedException();

        }

    
    }
}

