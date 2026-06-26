module RalphForge.Eval.ExprParse

open FParsec
open RalphForge.Eval.Contract

// Contract DSL for expressions, e.g.
//   0 <= attempts and attempts <= 3 and (@Locked -> attempts = 3)
// `@Name` is a control-state reference (AtState); bare identifiers are variables.

let private ws = spaces
let private tok p = p .>> ws
let private sym s = tok (pstring s)

let private isIdentStart c = isLetter c
let private isIdentCont c = isLetter c || isDigit c || c = '_'

/// A word operator (and/or/not) must not be glued to an identifier (e.g. "android").
let private afterWord = notFollowedBy (satisfy isIdentCont) >>. ws

let private opp = OperatorPrecedenceParser<Expr, unit, unit>()
let private pExpr = opp.ExpressionParser

let private pIdentRaw = many1Satisfy2L isIdentStart isIdentCont "identifier"

let private pName =
    tok pIdentRaw
    >>= fun name ->
        match name with
        | "true" -> preturn (BoolLit true)
        | "false" -> preturn (BoolLit false)
        | "and"
        | "or"
        | "not" -> fail "operator keyword in operand position"
        | _ -> preturn (Var name)

let private pIntLit = tok (pint32 |>> IntLit)
let private pAtState = tok (pchar '@') >>. tok pIdentRaw |>> AtState
let private pParens = between (sym "(") (sym ")") pExpr
let private pAtom = choice [ pParens; pAtState; pIntLit; pName ]

// Configure the precedence parser (forced at module init).
// Precedence ascending (binds tighter): -> < or < and < not < {= < <=} < {+ -}.
let private _configure =
    opp.TermParser <- pAtom
    opp.AddOperator(InfixOperator("->", ws, 1, Associativity.Right, fun a b -> Implies(a, b)))
    opp.AddOperator(InfixOperator("or", afterWord, 2, Associativity.Left, fun a b -> Or [ a; b ]))
    opp.AddOperator(InfixOperator("and", afterWord, 3, Associativity.Left, fun a b -> And [ a; b ]))
    opp.AddOperator(PrefixOperator("not", afterWord, 4, true, fun a -> Not a))
    opp.AddOperator(InfixOperator("<=", ws, 5, Associativity.None, fun a b -> Le(a, b)))
    opp.AddOperator(InfixOperator("<", notFollowedBy (pchar '=') >>. ws, 5, Associativity.None, fun a b -> Lt(a, b)))
    opp.AddOperator(InfixOperator("=", ws, 5, Associativity.None, fun a b -> Eq(a, b)))
    opp.AddOperator(InfixOperator("+", ws, 6, Associativity.Left, fun a b -> Add(a, b)))
    opp.AddOperator(InfixOperator("-", ws, 6, Associativity.Left, fun a b -> Sub(a, b)))

let private pFull = ws >>. pExpr .>> eof

/// Parse an expression in the contract DSL into the Expr AST.
let parse (input: string) : Result<Expr, string> =
    match run pFull input with
    | Success(e, _, _) -> Result.Ok e
    | Failure(msg, _, _) -> Result.Error msg
