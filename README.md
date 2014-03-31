# WF2oWFN (Windows Workflow to Open Workflow Nets)

WF2oWFN is a tool to translate Windows Workflow 4 (WF) processes into Open Workflow Nets (oWFN).
Both Xaml workflows and Xamlx workflow services are supported.
WF2oWFN integrates with a variety of tools from [service-technology.org](http://service-technology.org) to check for behavioral properties like deadlocking, controllability or automatic adapter synthesis. 

A compiled [binary distribution](https://github.com/uniba-dsg/wf2owfn/blob/master/wf2owfn-0.1.zip) and a large [test collection](https://github.com/uniba-dsg/wf2owfn/blob/master/compiler-testcases.zip) with sample outputs is available.
Moreover, we also supply an inofficial [WF Xaml specification](https://github.com/uniba-dsg/wf2owfn/blob/master/wf-xaml-vocabulary-specification.pdf) that is used by our compiler. 

More information can be found in the paper [Bridging the Heterogeneity of Orchestrations - A Petri Net-based Integration of BPEL and Windows Workflow](http://www.uni-bamberg.de/pi/bereich/forschung/publikationen/12-03-lenhard-wirtz-kolb/) or in the thesis [Integration heterogener Prozesssysteme in Service-orientierten Architekturen](https://github.com/uniba-dsg/wf2owfn/blob/master/integration-heterogener-prozessbasierter-systeme.pdf). The [presentation](https://github.com/uniba-dsg/wf2owfn/blob/master/soca-12-presentation.pdf) held at SOCA '12 also gives a short overview over the work.

WF2oWFN is licensed under the LGPL Version 3 Open Source License.

## Software Requirements
### Binary Distribution
- Windows XP SP3 or higher (see [.NET 4 Framework](http://www.microsoft.com/en-us/download/details.aspx?id=17718))
- [.NET Framework Version 4 Platform Update 1](http://msdn.microsoft.com/en-us/library/hh290669) or higher in *4.x* revision
- Optional: [Graphviz DOT](http://www.graphviz.org/) - Used for generating layouted graphs. Therefore `dot.exe` should be listed in `PATH` environment variable. 
  
### Source Distribution
- Windows XP SP3 or higher (see [.NET 4 Framework](http://www.microsoft.com/en-us/download/details.aspx?id=17718))
- Visual Studio 2010
- [.NET Framework Version 4 Platform Update 1](http://msdn.microsoft.com/en-us/library/hh290669) or higher in *4.x* revision
- *External dependencies*
    - [log4net](http://csharp-source.net/open-source/logging/log4net) - Used for logging capabilities
    - [TestAPICore](http://testapi.codeplex.com/) - Used for commandline parsing. 
    - [PNAPI](http://download.gna.org/service-tech/pnapi/) - Used for Graphviz [DOT](http://www.graphviz.org/doc/info/lang.html) output.
    - [Graphviz DOT](http://www.graphviz.org/) - Used for generating layouted graphs. Therefore `dot.exe` should be listed in `PATH` environment variable. 

## Compilation

### Buildorder
*WF2oWFN Module API > WF2oWFN > WF2oWFN Standard Activities*

### Filesystem structure:

    Root
    -----| Libs # compiled libraries (e.g. 'WF2oWFN API', 'log4net', ...)
    -----| Modules # compiled modules (e.g. 'Standard Activities')
    WF2oWFN.exe # compiled runtime

## Usage

Default invocation:

```bash
wf2owfn /i=inputfile /o=outputfile /f=owfn
```

Parameter:

```bash
wf2owfn [/i=FILE] [/f=FORMAT] [/o=FILE] [/h] [/v] [/d]
```

    /h         Print help and exit
    /v         Print version and exit
    /d         Append debug information
    /i=FILE    Read a WF process from FILE
    /f=FORMAT  Create output in given FORMAT; 
               values='owfn', 'dot', 'png' default='owfn'
    /o=FILE    Write output to FILE

Log-level configuration in `WF2oWFN.exe.config`:

	  <root>
	    <level value="INFO"/>
	    <appender-ref ref="ColoredConsoleAppender"/>
	  </root>

Possible values:  `OFF`, `ERROR`, `WARN`, `INFO`, `DEBUG` (default = `INFO`)

## Licensing
LGPL Version 3: [http://www.gnu.org/licenses/lgpl-3.0.html](http://www.gnu.org/licenses/lgpl-3.0.html)

# Authors

[Stefan Kolb](http://www.uni-bamberg.de/pi/team/kolb-stefan/)
