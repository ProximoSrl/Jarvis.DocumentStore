﻿using System;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public class OutOfProcessTikaJob : AbstractTikaOutOfProcessJob
    {
        public OutOfProcessTikaJob(
            ContentFormatBuilder builder,
            ContentFilterManager filterManager)
            : base(builder, filterManager)
        {
        }

        protected override ITikaAnalyzer BuildAnalyzer(Int32 analyzerOrdinal)
        {
            switch (analyzerOrdinal)
            {
                case 0:
                    return new TikaAnalyzer(JobsHostConfiguration)
                    {
                        Logger = this.Logger
                    };
                case 1:
                    return new TikaNetAnalyzer()
                    {
                        Logger = this.Logger
                    };
            }

            return null;
        }

        public override bool IsActive
        {
            get { return !base.JobsHostConfiguration.UseEmbeddedTika; }
        }
    }
}
