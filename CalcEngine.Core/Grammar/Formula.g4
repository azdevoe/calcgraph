// ═══════════════════════════════════════════════════════════════
// Formula.g4 — ANTLR4 combined grammar for CalcEngine
// Matches Design Portfolio Sections 3.2 (parser) and 3.3 (lexer).
// ═══════════════════════════════════════════════════════════════

grammar Formula;

// ---------- entry point ----------
formula      : '=' expr EOF                    # FormulaEntry
             | literal EOF                     # LiteralEntry
             ;

expr         : comparison ;

// ---------- precedence chain, loosest to tightest ----------
comparison   : comparison op=( '=' | '<>' | '<' | '<=' | '>' | '>=' ) addition
             | addition
             ;

addition     : addition op=( '+' | '-' ) multiply
             | multiply
             ;

multiply     : multiply op=( '*' | '/' ) unary
             | unary
             ;
                
unary        : op=( '-' | '+' ) unary
             | power
             ;

power        : atom ( '^' unary )? ;

atom         : NUMBER                          # NumberAtom
             | STRING                          # StringAtom
             | BOOLEAN                         # BooleanAtom
             | CELLREF                         # CellRefAtom
             | RANGE                           # RangeAtom
             | functionCall                    # CallAtom
             | '(' expr ')'                    # ParenAtom
             ;

functionCall : FUNCNAME '(' argList? ')' ;
argList      : arg ( ',' arg )* ;
arg          : RANGE | expr ;

literal      : NUMBER | STRING | BOOLEAN ;

// ═══════════════════════════════════════════════════════════════
// Lexer rules
// ═══════════════════════════════════════════════════════════════

BOOLEAN  : 'TRUE' | 'FALSE' ;

RANGE    : LETTERS DIGITS ':' LETTERS DIGITS ;
CELLREF  : LETTERS DIGITS ;
FUNCNAME : [A-Z] [A-Z0-9_.]* ;

NUMBER   : DIGITS ( '.' DIGITS )? ( [eE] [+-]? DIGITS )? ;
STRING   : '"' ( ~["] | '""' )* '"' ;

fragment LETTERS : [A-Z]+ ;
fragment DIGITS  : [0-9]+ ;

WS       : [ \t\r\n]+ -> skip ;