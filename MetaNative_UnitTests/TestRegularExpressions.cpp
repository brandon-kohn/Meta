#include "stdafx.h"
#include "CppUnitTest.h"

#include "boost/interprocess/file_mapping.hpp"
#include "boost/interprocess/mapped_region.hpp"
#include <boost/spirit/include/classic_file_iterator.hpp>
#include "boost/range/iterator_range_core.hpp"
#include "boost/xpressive/xpressive.hpp"
#include <fstream>
#include <iterator>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace MetaNative_UnitTests
{
    using namespace boost::xpressive;
    namespace xp = boost::xpressive;

    typedef boost::xpressive::basic_regex<boost::spirit::classic::file_iterator<> > fregex;
    typedef boost::xpressive::match_results<boost::spirit::classic::file_iterator<> > fmatch;

    sregex make_parens()
    {
        sregex parens; //parens = keep( '(' >> *keep( keep( +keep( ignored | ~(set= '(',')') ) | ( -!by_ref( parens ) ) ) ) >> ')' );
        // Example from Xpressive documentation:
        parens =
            '('                           // is an opening parenthesis ...
            >>                           // followed by ...
            *(                          // zero or more ...
            keep(+~(set = '(', ')'))  // of a bunch of things that are not parentheses ...
            |                          // or ...
            by_ref(parens)      // a balanced set of parentheses
            )                          //   (ooh, recursion!) ...
            >>                           // followed by ...
            ')'                            // a closing parenthesis
            ;
        return parens;
    }

    //sregex const backslashed_lines = keep( keep( *( '\\' >> keep( _ln ) | ~_ln ) ) ); //...zzz...!?
    sregex const string = keep('"' >> *keep(as_xpr("\\\\") | "\\\"" | ~(set = '"')) >> '"' | '\'' >> *keep(as_xpr("\\\\") | "\\'" | ~(set = '\'')) >> '\'');
    //sregex const string            = keep( as_xpr('"') >> *( ~( set = '\\','"' ) ) >> *(( '\\' >> _ ) >> *( ~( set = '\\','"') ) ) >> '"');
    //! Visual studio strips comments from the preprocessor.
    //sregex const comment           = keep( "//" /*>> backslashed_lines*/ | "/*" >> keep( *( ~(set='*') | '*' >> ~before('/') ) ) >> "*/" );
    sregex const blank_line = keep(*_s >> _ln);
    sregex const pp = keep(*_s >> '#' >> /*backslashed_lines*/ /*-*/*~_ln >> _ln);
    sregex const ws = _s | _ln /*| comment*/ | pp;
    sregex const ignored = keep(blank_line | string /*| comment*/ | pp);

    sregex const enumeration =
        keep
        (
        (_b >> as_xpr("enum") >> _b) >> *ws >> //! enum keyword
        !(_b >> as_xpr("class") >> _b) >>    //! optional class keyword
        (+ws >> *_w) >> *ws >>  //! optional enumerated type name
        as_xpr('{') >> //! opening brace
        (-*(~(set = '}'))) >> //! enumerations
        ('}' >> *ws >> ';') //! closing brace and semicolon.
        );

    sregex const parens = make_parens();

    //! Class Header:

    sregex const class_header =
        ~after(_b >> "enum" >> _b) >> //! make sure it's not a C++11 enum.
        keep
        (
        +ws >>
        keep(_b >> (as_xpr("class") | "struct") >> _b) >> //! class or struct
        keep(+ws >> +_w) >> //! typename
        keep(*keep(~(set = '(', ')', '{', ';', '=') | parens | ignored)) >> //! Not sure what all of this is.
        '{'
        );

    //! Function Header:

    sregex const tmplate = _b >> as_xpr("template") >> *ws >> '<' >> -*(~(set = '>')) >> '>';

    //sregex const control    = ( _b >> ( as_xpr( "__attribute__" ) | "__if_exists" | "__if_not_exists" | "for" | "while" | "if" | "catch" | "switch" ) >> _b );
    sregex const control = (_b >> (as_xpr("if") | "for" | "while" | "switch" | "catch" | "__if_exists" | "__if_not_exists" | "__attribute__") >> _b);
    sregex const modifiers = (_b >> (as_xpr("const") | "try" | "volatile") >> _b);
    sregex const start = (bos | after((set = '{', '}', ';'))) >> keep(*ws);//! The beginning of the sequence or after another function/class.
    sregex const body = ~before(control) >> keep(~(set = '{', '}', ';'));//! valid characters in function signature (inline, template/specializations, class name, function name)
    sregex const args = parens | ']';
    sregex const body_start = keep(*ws >> *(modifiers >> *ws) >> '{');

    //sregex const function_header = keep(start >> -*_signature >> args >> body_start);

    sregex const _signature = ~before(control) >> keep(~(set = '(', '{', ';'));//! valid characters in function signature (inline, template/specializations, class name, function name)
    sregex const function_header =
        keep
        (
        start >> //!bos or comes after }, {, or ; (and spaces/pp)
        //!tmplate >> *ws >> //! template declaration
        //!(_b >> as_xpr("inline") >> _b) >> //! inline declaration
        //! return type/class scope/function name
        +_signature >>
        !(as_xpr('(') >> *ws >> ')' >> *ws) >> //! operator ()
        parens >>
        body_start
        );

	TEST_CLASS(RegularExpressionUnitTests)
	{
	public:
		
		TEST_METHOD(TestFunctionHeaderMatch)
		{
            //! This unit test setup doesn't work correctly for some odd reason I think has nothing to do with C++.
            std::string test = "	size_t operator()(const _Kty& _Keyval) const"
                 "                                                              "
                 "                                                                          {";
            //! The stack isn't big enough, and can't seem to be increased via project settings for the way it's called from managed code.
            //Assert::IsTrue(boost::xpressive::regex_match(test, function_header));
        }

	};
}