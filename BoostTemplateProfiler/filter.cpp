////////////////////////////////////////////////////////////////////////////////
///
/// filter.cpp
/// ----------
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

#include "filter.hpp"

#include <string>
#include <cstdio>
#include <cstring>
//------------------------------------------------------------------------------
namespace boost
{
//------------------------------------------------------------------------------

namespace
{
    char const search[] = "template_profiler";

    char const back_trace_search[] =
    #if defined( _MSC_VER )
        "see reference to";
    #elif defined( __GNUC__ )
        "instantiated from";
    #else
        #error only Microsoft and gcc are supported.
    #endif
} // anonymous namespace

void copy_flat_only( std::string const & input, std::string & output )
{
    output.reserve( input.size() );

    unsigned int pos    (     0 );
    unsigned int counter(     0 );
    bool         matched( false );

    std::string buffer;

    std::string::const_iterator       p_ch     ( input.begin() );
    std::string::const_iterator const input_end( input.end  () );

    while ( p_ch != input_end )
    {
        char const ch( *p_ch++ );
        buffer.push_back( ch );
        if ( ch == '\n' )
        {
            if ( matched )
            {
                output.append( buffer );
                ++counter;
                #ifdef _MSC_VER
                    if ( counter % 400 == 0 ) std::fprintf( stderr, "On Instantiation %d\n", counter/4 );
                #else
                    if ( counter % 200 == 0 ) std::fprintf( stderr, "On Instantiation %d\n", counter/2 );
                #endif
            }
            buffer.clear();
            matched = false;
        }
        if ( ch == search[ pos ] )
        {
            ++pos;
            if ( search[ pos ] == '\0' )
            {
                matched = true;
            }
        }
        else
        {
            pos = 0;
        }
    }
}

void copy_call_graph( std::string const & input, std::string & output )
{
#if defined(_MSC_VER) && 0
    output.reserve( input.size() );

    unsigned int pos    (     0 );
    unsigned int counter(     0 );
    bool         matched( false );

    std::string buffer;

    std::string::const_iterator       p_ch     ( input.begin() );
    std::string::const_iterator const input_end( input.end  () );

    while ( p_ch != input_end )
    {
        char const ch( *p_ch++ );
        buffer.push_back( ch );
        if ( ch == '\n' )
        {
            if ( matched )
            {
                output.append( buffer );
                if ( ++counter % 200 == 0 ) std::fprintf( stderr, "On Instantiation %d\n", counter/2 );
                buffer.clear();
                matched = false;
                // process instantiation back-trace
                pos = 0;
                while ( p_ch != input_end )
                {
                    char const ch( *p_ch++ );
                    if ( ch == ' ' )
                    {
                        buffer.push_back( ch );
                        while ( p_ch != input_end )
                        {
                            char const ch( *p_ch++ );
                            buffer.push_back( ch );
                            if ( ch == '\n' )
                            {
                                if ( matched )
                                {
                                    output.append( buffer );
                                }
                                buffer.clear();
                                matched = false;
                                pos = 0;
                                break;
                            }
                            if ( ch == back_trace_search[ pos ] )
                            {
                                ++pos;
                                if ( back_trace_search[ pos ] == '\0' )
                                {
                                    matched = true;
                                }
                            }
                            else
                            {
                                pos = 0;
                            }
                        }
                    }
                    else
                    {
                        --p_ch;
                        break;
                    }
                }
            }
            buffer.clear();
            matched = false;
            pos = 0;
        }
        if ( ch == search[ pos ] )
        {
            ++pos;
            if ( search[ pos ] == '\0' )
            {
                matched = true;
            }
        }
        else
        {
            pos = 0;
        }
    }
#elif defined(__GNUC__) || 1
    // trying to figure out what we should copy is too hard.
    output = input;
#else
    #error Unknown compiler
#endif
}

//------------------------------------------------------------------------------
} // namespace boost
//------------------------------------------------------------------------------
