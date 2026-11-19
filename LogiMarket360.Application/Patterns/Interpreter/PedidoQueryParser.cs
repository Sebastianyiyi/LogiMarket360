namespace LogiMarket360.Application.Patterns.Interpreter;

using System.Globalization;
using System.Text.RegularExpressions;

public class PedidoQueryParser
{
    public IExpression Parse(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new FormatException("La consulta de filtro no puede ser vacía.");
        }

        var tokens = Tokenize(query);
        if (tokens.Count == 0)
        {
            throw new FormatException("No se encontraron elementos válidos en la consulta.");
        }

        int index = 0;
        var expression = ParseOr(tokens, ref index);

        if (index < tokens.Count)
        {
            throw new FormatException($"Sintaxis no válida: token inesperado '{tokens[index]}' al final de la consulta.");
        }

        return expression;
    }

    private static List<string> Tokenize(string query)
    {
        // Expresión regular que separa paréntesis, operadores relacionales y palabras/literales
        var regex = new Regex(@"\(|\)|>=|<=|=|>|<|AND|OR|[^\s()>=<]+", RegexOptions.IgnoreCase);
        var matches = regex.Matches(query);
        return matches.Select(m => m.Value).ToList();
    }

    private IExpression ParseOr(List<string> tokens, ref int index)
    {
        var left = ParseAnd(tokens, ref index);

        while (index < tokens.Count && tokens[index].Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            index++; // consumir 'OR'
            var right = ParseAnd(tokens, ref index);
            left = new OrExpression(left, right);
        }

        return left;
    }

    private IExpression ParseAnd(List<string> tokens, ref int index)
    {
        var left = ParsePrimary(tokens, ref index);

        while (index < tokens.Count && tokens[index].Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            index++; // consumir 'AND'
            var right = ParsePrimary(tokens, ref index);
            left = new AndExpression(left, right);
        }

        return left;
    }

    private IExpression ParsePrimary(List<string> tokens, ref int index)
    {
        if (index >= tokens.Count)
        {
            throw new FormatException("Se esperaba una expresión o campo de filtro, pero se alcanzó el final de la consulta.");
        }

        if (tokens[index] == "(")
        {
            index++; // consumir '('
            var expr = ParseOr(tokens, ref index);

            if (index >= tokens.Count || tokens[index] != ")")
            {
                throw new FormatException("Error de sintaxis: falta el paréntesis de cierre ')'.");
            }

            index++; // consumir ')'
            return expr;
        }

        // Parsear predicado terminal: CAMPO OPERADOR VALOR
        if (index + 2 >= tokens.Count)
        {
            throw new FormatException($"Expresión de predicado incompleta en '{tokens[index]}'. Formato requerido: CAMPO OP VALOR.");
        }

        string campo = tokens[index].ToUpperInvariant();
        string operador = tokens[index + 1];
        string valor = tokens[index + 2];
        index += 3;

        return campo switch
        {
            "ESTADO" when operador == "=" => new EstadoTerminalExpression(valor),
            "TOTAL" when operador is "=" or ">=" or "<=" or ">" or "<" => 
                decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) ||
                decimal.TryParse(valor, NumberStyles.Any, CultureInfo.CurrentCulture, out num)
                    ? new TotalTerminalExpression(operador, num)
                    : throw new FormatException($"El valor '{valor}' para TOTAL no es un número decimal válido."),
            "CLIENTE" when operador == "=" => new ClienteTerminalExpression(valor),
            _ => throw new FormatException($"Campo u operador no válido en predicado: '{campo} {operador} {valor}'.")
        };
    }
}
