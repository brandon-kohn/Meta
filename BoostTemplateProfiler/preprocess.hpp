////////////////////////////////////////////////////////////////////////////////
///
/// \file preprocess.hpp
/// --------------------
///
/// Copyright (c) 2011 Domagoj Saric
///
///  Use, modification and distribution is subject to the Boost Software License, Version 1.0.
///  (See accompanying file LICENSE_1_0.txt or copy at
///  http://www.boost.org/LICENSE_1_0.txt)
///
/// For more information, see http://www.boost.org
///
////////////////////////////////////////////////////////////////////////////////
//------------------------------------------------------------------------------
#ifndef preprocess_hpp__A0C9BD63_9A59_4F40_A70E_0026369C14C0
#define preprocess_hpp__A0C9BD63_9A59_4F40_A70E_0026369C14C0
#pragma once

#include "export_api.hpp"
//------------------------------------------------------------------------------
#include <string>
//------------------------------------------------------------------------------

//! Preprocess filename.i file and write it to the specified output.
extern "C" void TEMPLATE_PROFILER_API TemplateProfilePreprocess( char const* p_filename, char const* p_output_filename );

namespace boost
{
//------------------------------------------------------------------------------
void preprocess( char const * p_filename, char const* p_output_file );

//------------------------------------------------------------------------------
} // namespace boost
//------------------------------------------------------------------------------
#endif // preprocess_hpp
