////////////////////////////////////////////////////////////////////////////////
///
/// \file preprocess.cpp
/// --------------------
///
/// Copyright (c) 2008.-2009. Steven Watanabe (preprocess.pl)
/// Copyright (c) 2011.       Domagoj Saric
///
///  Use, modification and distribution is subject to the
///  Boost Software License, Version 1.0.
///  (See accompanying file LICENSE_1_0.txt or copy at
///  http://www.boost.org/LICENSE_1_0.txt)
///
/// For more information, see http://www.boost.org
///
////////////////////////////////////////////////////////////////////////////////
//------------------------------------------------------------------------------
#undef BOOST_ENABLE_ASSERT_HANDLER

#define BOOST_XPRESSIVE_USE_C_TRAITS

#include "preprocess.hpp"
#include "filter.hpp"
#include "postprocess.hpp"

#include "boost/assert.hpp"
#include "boost/concept_check.hpp"
#include "boost/interprocess/file_mapping.hpp"
#include "boost/interprocess/mapped_region.hpp"
 #include <boost/spirit/include/classic_file_iterator.hpp>
#include "boost/range/iterator_range_core.hpp"
#include "boost/xpressive/xpressive.hpp"
#include <boost/xpressive/regex_actions.hpp>
#include <boost/format.hpp>

#include <fstream>

#include <iterator>
//------------------------------------------------------------------------------
// namespace Rules
// {
//     boost::regex backslashed_lines = "qr/(?>(?>\\(?>\n|\r\n)|.)*)/";
//     boost::regex string = "qr/(?>\"(?>\\\\|\\\"|[^\"])*\"|'(?>\\\\|\\'|[^'])*')/";
//     boost::regex comment = "qr{(?>//$backslashed_lines|/\*(?>[^*]|\*(?!/))*\*/)}";
//     boost::regex pp = "qr/(?>#$backslashed_lines)/";
//     boost::regex ignored = "qr/(?>$string|$comment|$pp)/";
//     boost::regex parens = "qr/(?>\((?>(?>(?>$ignored|[^()])+)|(??{$parens}))*\))/";
//     boost::regex ws = "qr/(?:$comment|$pp|\s|(?:\\(?:\n|\r\n)))/";
//     boost::regex class_header = "qr/(?>(?>\b(?:class|struct))(?>$ws+\w+)(?>(?>[^(){;=]|$parens|$ignored)*)\{)/";
//     boost::regex control = "qr/(?:\b(?:__attribute__|__if_exists|__if_not_exists|for|while|if|catch|switch)\b)/";
//     boost::regex modifiers = "qr/(?:\b(?:try|const|volatile)\b)/";
//     boost::regex start = "qr/(?:^|\G|(?<=[{};]))(?>$ws*)/";
//     boost::regex body = "qr/(?:(?!$control)(?>$ignored|[^{};]))/";
//     boost::regex end = "qr/(?:$parens|\])/";
//     boost::regex body_start = "qr/(?>$ws*(?:$modifiers$ws*)*\{)/";
//     boost::regex function_header = "qr/(?>(?:$start)(?:$body*?$end)(?:$body_start))/";
// }

namespace boost
{
//------------------------------------------------------------------------------
namespace regex
{
    using namespace boost::xpressive;

    typedef boost::xpressive::basic_regex<boost::spirit::classic::file_iterator<> > fregex;
    typedef boost::xpressive::match_results<boost::spirit::classic::file_iterator<> > fmatch;

    cregex make_parens()
    {
        cregex parens;// = keep('(' >> *keep(keep(+keep(ignored | ~(set = '(', ')')) | (-!by_ref(parens)))) >> ')');
        // Example from Xpressive documentation:
        parens =
        '('                           // is an opening parenthesis ...
         >>                           // followed by ...
          *(                          // zero or more ...
             keep( +~(set='(',')') )  // of a bunch of things that are not parentheses ...
           |                          // or ...
             by_ref(parens)      // a balanced set of parentheses
           )                          //   (ooh, recursion!) ...
         >>                           // followed by ...
        ')'                            // a closing parenthesis
        ;
        return parens;
    }

    //cregex const backslashed_lines = keep( keep( *( '\\' >> keep( _ln ) | ~_ln ) ) ); //...zzz...!?
    cregex const string            = keep( '"' >> *keep( as_xpr( "\\\\" ) | "\\\"" | ~(set='"') ) >> '"' | '\'' >> *keep( as_xpr( "\\\\" ) | "\\'" | ~(set='\'') ) >> '\'' );
    //cregex const string            = keep( as_xpr('"') >> *( ~( set = '\\','"' ) ) >> *(( '\\' >> _ ) >> *( ~( set = '\\','"') ) ) >> '"');
    //! Visual studio strips comments from the preprocessor.
    //cregex const comment           = keep( "//" /*>> backslashed_lines*/ | "/*" >> keep( *( ~(set='*') | '*' >> ~before('/') ) ) >> "*/" );
    cregex const pp                = keep( '#' >> /*backslashed_lines*/ /*-*/*~_ln >> _ln );
    cregex const ignored           = keep( string /*| comment*/ | pp );
    cregex const parens            = keep('(' >> *keep(keep(+keep(ignored | ~(set = '(', ')')) | (-!by_ref(parens)))) >> ')'); //make_parens();
    cregex const ws                = _s | _ln /*| comment*/ | pp;
    cregex const cstart = keep( after((set = '{', '}', ';','>')) >> *ws );
    cregex const enum_class = (_b >> as_xpr("enum") >> +ws >> "class" >> _b);
    cregex const class_header =
        //cstart >> 
        keep
        (
            //cstart >>
            keep( _b >> ( as_xpr( "class" ) | "struct" ) ) >>
            keep( +ws >> +_w ) >>
            keep( *keep( ~(set= '(',')','{',';','=') | parens | ignored ) ) >>
            '{'
        );

    //cregex const control    = ( _b >> ( as_xpr( "__attribute__" ) | "__if_exists" | "__if_not_exists" | "for" | "while" | "if" | "catch" | "switch" ) >> _b );
    cregex const control    = ( _b >> ( as_xpr( "if" ) | "for" | "while" | "switch" | "catch" | "__if_exists" | "__if_not_exists" | "__attribute__" ) >> _b );
    cregex const modifiers  = ( _b >> ( as_xpr( "const" ) | "try" | "volatile" ) >> _b );
    cregex const fstart      = ( bos /*| cregex::compile( "\\G" )*/ | after( (set= '{','}',';') ) ) >> keep( *ws );
    cregex const body       = ~before( control ) >> keep( ignored | ~(set= '{','}',';') );
    cregex const end        = parens | ']';
    cregex const body_start = keep( *ws >> *(modifiers >> *ws) >> '{' );

    cregex const function_header = keep( fstart >> ( *body >> end ) >> body_start );
} // namespace regex

struct formatter : boost::noncopyable
{
    template<typename Out>
    Out operator()( regex::cmatch const & what, Out out ) const
    {
        using namespace regex;

        typedef cmatch::value_type sub_match;

        BOOST_ASSERT( what.size() == 6 );

        cmatch::const_iterator const p_match( std::find_if( what.begin() + 1, what.end(), []( sub_match const & match ){ return match.matched; } ) );
        BOOST_ASSERT_MSG( p_match != what.end(), "Something should have matched." );
        sub_match const & match( *p_match );

        enum match_type_t
        {
            ignore = 1,
            class_header,
            func_header,
            open_brace,
            close_brace
        };

        unsigned int const match_type( p_match - what.begin() );
        switch ( match_type )
        {
            case ignore:
                out = std::copy( match.first, match.second, out );
                break;

            case class_header:
            {
                braces.push_back(" TEMPLATE_PROFILE_EXIT() }");
                static char const tail[] = " TEMPLATE_PROFILE_ENTER()";
                out = std::copy( match.first, match.second, out );
                out = std::copy( boost::begin( tail ), boost::end( tail ) - 1, out );
                break;
            }

            case func_header:
            {
                braces.push_back(" TEMPLATE_PROFILE_EXIT() }");
                static char const tail[] = " TEMPLATE_PROFILE_ENTER()";
                out = std::copy(match.first, match.second, out);
                out = std::copy(boost::begin(tail), boost::end(tail) - 1, out);
                break;
            }

            case open_brace:
                braces.push_back( "}" );
                out = std::copy( match.first, match.second, out );
                break;

            case close_brace:
                out = std::copy( braces.back().begin(), braces.back().end(), out );
                braces.pop_back();
                break;

            default:
                BOOST_ASSERT( false );
                break;
        }

        return out;
    }

    mutable std::vector<std::string> braces;
};

void preprocess(char const * const p_filename, const char* const p_output_file)
{
    using namespace boost;

    interprocess::mapped_region const input_file_view
    (
        interprocess::file_mapping
        (
            p_filename,
            interprocess::read_only
        ),
        interprocess::read_only
    );

    iterator_range< char const * > input
    (
        static_cast<char const *>( input_file_view.get_address() )
      , static_cast<char const *>( input_file_view.get_address() ) + input_file_view.get_size()
    );

    regex::match_results<char const *> search_results;
    using namespace regex;
    static const cregex  main_regex( (s1 = (ignored | keep(enum_class))) | (s2 = keep(class_header)) | (s3 = keep(function_header)) | (s4 = '{') | (s5 = '}') );
    
#if (0)//defined(_DEBUG)
    std::ofstream logofs("g:\\Projects\\regex_log.log");
    boost::format fmt("//! regex: %1% : %2%");
#define META_LOG_REGEX(logofs, r) \
    logofs << boost::str(fmt % BOOST_PP_STRINGIZE(r) % r.regex_id()) << std::endl;\
    /***/

    META_LOG_REGEX(logofs, string);
    META_LOG_REGEX(logofs, pp);
    META_LOG_REGEX(logofs, ignored);
    META_LOG_REGEX(logofs, parens);
    META_LOG_REGEX(logofs, ws);
    META_LOG_REGEX(logofs, cstart);
    META_LOG_REGEX(logofs, enum_class);
    META_LOG_REGEX(logofs, class_header);
    META_LOG_REGEX(logofs, control);
    META_LOG_REGEX(logofs, modifiers);
    META_LOG_REGEX(logofs, fstart);
    META_LOG_REGEX(logofs, body);
    META_LOG_REGEX(logofs, boost::regex::end);
    META_LOG_REGEX(logofs, body_start);
    META_LOG_REGEX(logofs, function_header);
    META_LOG_REGEX(logofs, main_regex);

    logofs.close();

    //! Put this in regex_matcher.hpp (56)
    // #if (0)//defined(_DEBUG)
    //     xpression_adaptor<reference_wrapper<Next const>, matchable<BidiIter> > adaptor(boost::cref(next));
    //     bool result = push_context_match(this->impl_, state, adaptor);
    //     std::ofstream ofs("g:\\Projects\\regex_log.log", std::ios::app);
    //     if (ofs.good())
    //     {
    //         ofs << "---------------------------------------------------" << std::endl;
    //         ofs << "Regex (" << (result ? "M" : "U") << "): " << impl_.xpr_.get() << " at |" << std::string(state.cur_, state.cur_ + 50) << std::endl;
    //     }
    //     return result;
    // #else
    //     xpression_adaptor<reference_wrapper<Next const>, matchable<BidiIter> > adaptor(boost::cref(next));
    //     return push_context_match(this->impl_, state, adaptor);
    // #endif    

#endif

    //buffer = "#include <template_profiler.hpp>\n";
    std::string buffer =
        "\
        \n\
        namespace template_profiler {\n\
            struct incomplete_enter;\n\
            struct incomplete_exit;\n\
            template<int N>\n\
            struct int_ {\n\
                enum { value = N };\n\
                typedef int type;\n\
            };\n\
            template<class T>\n\
            struct make_zero {\n\
                enum { value = 0 };\n\
            };\n\
            extern int enter_value;\n\
            extern int exit_value;\n\
        }\n\
        \n\
        char template_profiler_size_one(...);\n\
        \n\
        #define TEMPLATE_PROFILE_ENTER() enum template_profiler_test_enter { template_profiler_value_enter = sizeof(delete ((::template_profiler::incomplete_enter*)0),0) };\n\
        #define TEMPLATE_PROFILE_EXIT() enum template_profiler_test_exit { template_profiler_value_exit = sizeof(delete ((::template_profiler::incomplete_exit*)0),0) };\n\
        ";
    std::ofstream ofs( p_output_file );
    if( !ofs.good() )
        return;
 
    ofs << buffer;
    // Implementation note:
    //   The whole file has to be searched at once in order to handle class/
    // function definitions over several lines.
    //                                    (01.08.2011.) (Domagoj Saric)

    regex_replace
    (
        std::ostream_iterator<const char>(ofs),
        input.begin(),
        input.end(),
        main_regex,
        formatter()
    );
}

//------------------------------------------------------------------------------
} // namespace boost

//! Preprocess filename.i file and write it to the specified output.
extern "C"
void TEMPLATE_PROFILER_API TemplateProfilePreprocess( char const* p_filename, char const* p_output_filename )
{
    using namespace boost;
    std::string preprocessed_input;
    std::string filtered_input;

    preprocess( p_filename, p_output_filename );
    /*
    //!Unfiltered
    std::string unfil( p_output_filename );
    std::ofstream ofs1(unfil + ".unf");
    if( ofs1.good() )
    {
        ofs1 << preprocessed_input << std::endl;
        ofs1.close();
    }

    copy_call_graph( preprocessed_input, filtered_input );

    std::ofstream ofs(p_output_filename);
    if( ofs.good() )
    {
        ofs << filtered_input << std::endl;
        ofs.close();
    }
    */
}

//------------------------------------------------------------------------------
