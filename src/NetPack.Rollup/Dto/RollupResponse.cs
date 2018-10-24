﻿using Newtonsoft.Json.Linq;

namespace NetPack.Rollup
{
    public class RollupResponse
    {
        public string[] Files { get; set; }

        public JValue Code { get; set; }

        public JValue SourceMap { get; set; }

        public JObject Echo { get; set; }

    }
}