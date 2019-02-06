# Tika analyzer

## General way of working

It could convert with a .NET based analyzer that uses IKVM (OutOfProcessTikaNetJob) or a job that directly
calls java machine with commandline (OutOfProcessTikaJob).

Configuration is done in file JobsHostConfiguration, for tika it checks environment variable JARVIS_DOCUMENTSTORE_TIKA_EMBEDDED.
If those variable is true, it will use IKVM and will use embedded tika (faster)