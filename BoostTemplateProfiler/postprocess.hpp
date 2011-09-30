////////////////////////////////////////////////////////////////////////////////
///
/// \file postprocess.hpp
/// ---------------------
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
#ifndef postprocess_hpp__AE416EDF_4F51_42bF_8F69_0BCDE2E5FE8D
#define postprocess_hpp__AE416EDF_4F51_42bF_8F69_0BCDE2E5FE8D
#pragma once

#include "export_api.hpp"

//------------------------------------------------------------------------------
typedef int (__stdcall *STRFPTR)(const char *str);
extern "C" void TEMPLATE_PROFILER_API TemplateProfilePostProcess( char const* input_file_name, STRFPTR logCallback );

namespace boost
{
//------------------------------------------------------------------------------

void postprocess
(
    char const * input_file_name
);

//------------------------------------------------------------------------------
} // namespace boost
//------------------------------------------------------------------------------
#endif // postprocess_hpp
