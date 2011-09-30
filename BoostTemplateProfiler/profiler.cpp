////////////////////////////////////////////////////////////////////////////////
///
/// profiler.cpp
/// ------------
///
/// Copyright (c) 2008-2009 Steven Watanabe
/// Copyright (c) 2011      Domagoj Saric
///
///  Use, modification and distribution is subject to the Boost Software License, Version 1.0.
///  (See accompanying file LICENSE_1_0.txt or copy at
///  http://www.boost.org/LICENSE_1_0.txt)
///
/// For more information, see http://www.boost.org
///
////////////////////////////////////////////////////////////////////////////////
//------------------------------------------------------------------------------
#undef BOOST_ENABLE_ASSERT_HANDLER

#include ".\export_api.hpp"

#include "filter.hpp"
#include "postprocess.hpp"
#include "preprocess.hpp"

#include "boost/assert.hpp"
#include "boost/config.hpp"
#include <boost/range.hpp>
#include <boost/shared_array.hpp>

// POSIX implementation
#if defined( BOOST_HAS_UNISTD_H )
    #include "unistd.h"
#elif defined( BOOST_MSVC )
    #pragma warning ( disable : 4996 ) // "The POSIX name for this item is deprecated. Instead, use the ISO C++ conformant name."
    #include "io.h"
    #include "windows.h"
#else
    #error unknown or no POSIX implementation
#endif // BOOST_HAS_UNISTD_H
#include "fcntl.h"
#include "sys/stat.h"
#include "sys/types.h"

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <exception>
#include "process.h"
#include <string>
#include <algorithm>
#include <tchar.h>
#include <stdio.h>
#include <strsafe.h>
#include <sstream>
#include <fstream>
#include <iostream>

template <typename Range>
boost::shared_array<typename boost::range_value<Range>::type> MakeSharedArray( Range r )
{
    typedef typename boost::range_value<Range>::type value_type;
    std::size_t size = std::distance(boost::begin(r), boost::end(r)) + 1;//for null term strings
    boost::shared_array<value_type> a( new value_type[size] );
    ZeroMemory( a.get(), size );
    std::copy( boost::begin(r), boost::end(r), a.get() );
    return a;
}

/*
void RunCommandInProcess( std::string args, std::string starting_directory )
{
    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    boost::shared_array<std::string::value_type> argArray( MakeSharedArray( args ) );

    ZeroMemory( &si, sizeof(STARTUPINFO) );
    si.cb = sizeof(STARTUPINFO);
    si.hStdError = g_hChildStd_OUT_Wr;
    si.hStdOutput = g_hChildStd_OUT_Wr;
    si.hStdInput = g_hChildStd_IN_Rd;
    si.dwFlags |= STARTF_USESTDHANDLES;

    ZeroMemory( &pi, sizeof(PROCESS_INFORMATION) );

    // Start the child process.
    if( !CreateProcess( NULL,   // No module name (use command line)
        argArray.get(),        // Command line
        NULL,           // Process handle not inheritable
        NULL,           // Thread handle not inheritable
        FALSE,          // Set handle inheritance to FALSE
        0,              // No creation flags
        NULL,           // Use parent's environment block
        starting_directory.c_str(),           // Use parent's starting directory
        &si,            // Pointer to STARTUPINFO structure
        &pi )           // Pointer to PROCESS_INFORMATION structure
        )
    {
        LPVOID lpMsgBuf;
        DWORD dw = GetLastError();

        FormatMessage(
            FORMAT_MESSAGE_ALLOCATE_BUFFER |
            FORMAT_MESSAGE_FROM_SYSTEM |
            FORMAT_MESSAGE_IGNORE_INSERTS,
            NULL,
            dw,
            MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
            (LPTSTR) &lpMsgBuf,
            0, NULL );

        // Display the error message and exit the process
        std::cout << "CreateProcess failed " << lpMsgBuf << std::endl;
        LocalFree(lpMsgBuf);

        return;
    }

    // Wait until child process exits.
    WaitForSingleObject( pi.hProcess, INFINITE );

    // Close process and thread handles.
    CloseHandle( pi.hProcess );
    CloseHandle( pi.hThread );
}
*/

// Shell Execute
template <typename String>
bool RunCommandInProcess(String strCommandPath, String strOptions, String startingDirectory )
{
    /*
    typedef typename boost::range_value<String>::type char_t;
    boost::shared_array<typename boost::range_value<String>::type> strQCommandPath( MakeSharedArray( AddQuotes(strCommandPath) ) );
    boost::shared_array<typename boost::range_value<String>::type> args( MakeSharedArray( strOptions ) );
    boost::shared_array<typename boost::range_value<String>::type> dir( MakeSharedArray( startingDirectory ) );

    SHELLEXECUTEINFO ShellInfo; // Name structure

    memset(&ShellInfo, 0, sizeof(ShellInfo)); // Set up memory block
    ShellInfo.cbSize = sizeof(ShellInfo); // Set up structure size
    ShellInfo.hwnd = 0; // Calling window handle
    ShellInfo.lpVerb = _T("open");
    ShellInfo.lpFile = strQCommandPath.get();
    ShellInfo.fMask = SEE_MASK_NOCLOSEPROCESS; //| SEE_MASK_NOASYNC | SEE_MASK_WAITFORINPUTIDLE;
    ShellInfo.lpParameters = args.get();
    ShellInfo.lpDirectory = dir.get();
    bool res = ShellExecuteEx(&ShellInfo); // Call to function
    if (!res)
    {
        LPVOID lpMsgBuf;
        DWORD dw = GetLastError();

        FormatMessage(
            FORMAT_MESSAGE_ALLOCATE_BUFFER |
            FORMAT_MESSAGE_FROM_SYSTEM |
            FORMAT_MESSAGE_IGNORE_INSERTS,
            NULL,
            dw,
            MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
            (LPTSTR) &lpMsgBuf,
            0, NULL );

        // Display the error message and exit the process
        std::cout << "CreateProcess failed " << lpMsgBuf << std::endl;
        LocalFree(lpMsgBuf);
        return false;
    }

    WaitForSingleObject(ShellInfo.hProcess, INFINITE); // wait forever for process to finish
    //WaitForInputIdle(ShellInfo.hProcess, INFINITE);

    CloseHandle( ShellInfo.hProcess);
    */
    return true;
}

std::string RemoveQuotes( std::string s )
{
    s.erase( std::remove( s.begin(), s.end(), '\"' ), s.end() );
    return s;
}

std::string AddQuotes( std::string s )
{
    std::string quote("\"");
    return quote + s + quote;
}

extern "C"
int TEMPLATE_PROFILER_API ProfileTemplate( char const * const compiler_binary_, char const * const compiler_args_, char const * const starting_directory_ , char const * const source_to_profile_, char const* const output_profile_ )
{
    using namespace boost;

    std::string compiler_binary( compiler_binary_ );
    std::string compiler_args( compiler_args_ );
    std::string source_to_profile( source_to_profile_ );
    std::string output_profile(output_profile_);
    std::string starting_directory( starting_directory_ );

    try
    {
        std::string const full_command_line( AddQuotes(compiler_binary) + " " + AddQuotes(source_to_profile) + " " + compiler_args + " > " + AddQuotes( output_profile ) );
        int const result( ::system( full_command_line.c_str() ) );
        if ( result != 0 )
            return EXIT_FAILURE;

        return EXIT_SUCCESS;
    }
    catch ( std::exception const & e )
    {
        std::puts( e.what() );
        return EXIT_FAILURE;
    }
    catch ( ... )
    {
        return EXIT_FAILURE;
    }
}

//------------------------------------------------------------------------------
namespace boost
{
//------------------------------------------------------------------------------
extern "C"
int main( int const argc, char const * const argv[] )
{
    if ( argc != ( 1 + 4 ) )
    {
        std::puts
        (
            "Incorrect number of arguments.\n"
            "Use:\n"
            "profiler\n"
                "\t<compiler binary path>\n"
                "\t<compiler response file with options to build the target source with>\n"
                "\t<source to profile>\n"
                "\t<result file name>."
        );
        return EXIT_FAILURE;
    }

    char const * const compiler_binary       ( argv[ 1 ] );
    char const * const compiler_response_file( argv[ 2 ] );
    char const * const source_to_profile     ( argv[ 3 ] );
    char const * const result_file           ( argv[ 4 ] );

    try
    {
        static char const compiler_preprocessed_file[] = "template_profiler.compiler_preprocessed.i";

        {
            std::string const full_command_line( std::string( compiler_binary ) + " " + source_to_profile + " @" + compiler_response_file + " -E > " + compiler_preprocessed_file );
            int const result( /*std*/::system( full_command_line.c_str() ) );
            if ( result != 0 )
            {
                std::puts( "Failed generating compiler preprocessed file." );
                return EXIT_FAILURE;
            }
        }

        static char const prepared_file_to_compile[] = "template_profiler.preprocessed.cpp";
        {
            std::string preprocessed_input;
            std::string filtered_input    ;
            //preprocess     ( compiler_preprocessed_file, preprocessed_input );
            copy_call_graph( preprocessed_input        , filtered_input     );

            int const file_id( /*std*/::open( prepared_file_to_compile, O_CREAT | O_TRUNC | O_WRONLY, S_IREAD | S_IWRITE ) );
            if ( file_id < 0 )
            {
                std::puts( "Failed creating an intermediate file." );
                return EXIT_FAILURE;
            }
            int const write_result( /*std*/::write( file_id, &filtered_input[ 0 ], filtered_input.size() ) );
            BOOST_VERIFY( /*std*/::close( file_id ) == 0 );
            if ( write_result < 0 )
            {
                std::puts( "Failed writing to an intermediate file." );
                return EXIT_FAILURE;
            }
        }

        static char const final_compiler_output[] = "template_profiler.final_compiler_output.txt";
        {
            std::string const full_command_line( std::string( compiler_binary ) + " " + prepared_file_to_compile + " @" + compiler_response_file + " > " + final_compiler_output );
            int const result( /*std*/::system( full_command_line.c_str() ) );
            if ( result != 0 )
            {
                std::puts( "Failed compiling an intermediate file." );
                return EXIT_FAILURE;
            }
        }

        postprocess( final_compiler_output );

        return EXIT_SUCCESS;
    }
    catch ( std::exception const & e )
    {
        std::puts( e.what() );
        return EXIT_FAILURE;
    }
    catch ( ... )
    {
        return EXIT_FAILURE;
    }
}

//------------------------------------------------------------------------------
} // namespace boost
//------------------------------------------------------------------------------
