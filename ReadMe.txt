Meta is a Visual Studio 2010 VSPackage extension which whose purpose is to provide utilities for template meta-programming in C++. The library is implemented in C# for the VSPackage components with an unmanaged C++ core for the utilities.
Features

Meta currently features the following utilities:

*    C++ project and file compile time profiling
*    C++ file template instantiation profiling

Details and Usage:

* Profile Build Time

The Profile Build Time command is used to measure the compile time of each C++ file in a C++ project. The times should be considered to represent an upper bound on the actual compile time of the file sans project level linking and optimizations (i.e. link time code generation etc.) The current implementation supports only the vc100 tool-set supplied with Visual Studio 2010.

The Profile Build Time command can be accessed through the context menu of C++ Projects and Project Items in the Solution Explorer window of the Visual Studio 2010 IDE. This is generally found through right-clicking on the nodes of the tree in the Solution Explorer view. While executing a build time profile other building utilities should be disabled. A currently executing build time profile may be canceled through a command accessed through the context menu as previously described.

* Profile Template Instantiations

The Profile Template Instantiations command is used to measure the number and location of template instantiations within a single translation unit. The command is accessed through the context menu of the Solution Explorer window when invoked from a C++ file node. The current implementation is limited to files using the vc100 tool-set. Profiling template instantiations is mutually exclusive of other build activities. A profile template instantiations command may be canceled by accessing the cancel command through the context menu of the file node.

* Acknowledgements

The C++ core library makes use of Boost (http://www.boost.org). Specifically the template_profiler2 code tool implemented by Steven Watanabe and Domagoj Saric. The Boost.Xpressive library by Eric Niebler is used for parsing and text manipulation.