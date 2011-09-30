////////////////////////////////////////////////////////////////////////////////
///
/// \file filter.hpp
/// ----------------
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
#ifndef filter_hpp__17451C87_D31A_43A8_B226_F0D430EB504E
#define filter_hpp__17451C87_D31A_43A8_B226_F0D430EB504E
#pragma once
//------------------------------------------------------------------------------
#include <string>
//------------------------------------------------------------------------------
namespace boost
{
//------------------------------------------------------------------------------

void copy_call_graph( std::string const & input, std::string & output );
void copy_flat_only ( std::string const & input, std::string & output );

//------------------------------------------------------------------------------
} // namespace boost
//------------------------------------------------------------------------------
#endif // filter_hpp
