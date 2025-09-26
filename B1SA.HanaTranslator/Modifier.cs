using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace B1SA.HanaTranslator
{
    public class ModifiedList : List<object>
    {
        public Object Peek()
        {
            return this.Last();
        }

        public void Push(Object node)
        {
            this.Add(node);
        }

        public Object Pop()
        {
            var ret = this.Last();
            this.Remove(ret);
            return ret;
        }
    }

    public class VariablesPool
    {
        private const string strBase = "temp_var_";
        private int id = 0;

        public VariablesPool()
        {
            id = 0;
        }

        public string GetNewVariableName()
        {
            return strBase + id++;
        }
    }

    public class ProceduresPool
    {
        private const string strBase = "temp_procedure_";
        private int id = 0;

        public ProceduresPool()
        {
            id = 0;
        }

        public string GetNewProcedureName()
        {
            return strBase + id++;
        }
    }
    public class Modifier : Scanner
    {
        private delegate GrammarNode CreateNewExprDelegate();

        #region Constants
        private const string ZERO_TIMESTAMP = "1900-01-01 00:00:00.000";
        #endregion

        #region Properties
        public BlockStatement Statement = null;

        private ModifiedList _NewParrent = [];
        private Stack<object> _OldParrent = new Stack<object>();
        private VariablesPool VarPool = new VariablesPool();
        public ProceduresPool ProcPool = new ProceduresPool();
        private Config config;
        #endregion

        public Modifier(Config configuration)
        {
            config = configuration;
        }

        private CreateAlterProcedureStatement GetNearestProcedure()
        {
            foreach (var obj in _NewParrent.Reverse<Object>()) {
                if (obj is CreateAlterProcedureStatement) {
                    return (obj as CreateAlterProcedureStatement);
                }
            }
            return null;
        }

        private BlockStatement GetNearestFather()
        {
            foreach (var obj in _NewParrent.Reverse<Object>()) {
                if (obj is BlockStatement) {
                    return (obj as BlockStatement);
                }
                if (obj is CreateAlterProcedureStatement) {
                    return (obj as CreateAlterProcedureStatement).Statements;
                }
            }
            return null;
        }

        private Statement GetNearestStatement()
        {
            foreach (var obj in _NewParrent.Reverse<Object>()) {
                if (obj is Statement) {
                    return (obj as Statement);
                }
            }
            return null;
        }

        private void MoveAllCommentsToNearestStatement(GrammarNode node)
        {
            if (node is Statement)
                return;

            if (node.Comments.Count == 0)
                return;

            var stmt = GetNearestStatement();
            if (stmt != null) {
                stmt.MoveCommentsFrom(node);
            }
        }

        public override void Scan(GrammarNode node)
        {
            if (node == null) {
                return;
            }

            // Clone node
            var newParrent = _NewParrent.Count > 0 ? _NewParrent.Peek() : null;
            var oldParrent = _OldParrent.Count > 0 ? _OldParrent.Peek() : null;
            object nodeClone = null;

            if (Statement == null && node is BlockStatement) {
                nodeClone = node.Clone();
                Statement = (nodeClone as BlockStatement);
            }
            else {
                var fiListParrent = newParrent.GetType().GetField("List");
                if (fiListParrent != null || newParrent is IList) {
                    var nodeType = node.GetType();
                    var toClone = true;

                    if (nodeType.IsGenericType) {
                        if (nodeType.GetGenericTypeDefinition().Name.Contains("GrammarNodeList")) {
                            var innerType = GetTemplateType(nodeType);
                            var clonedTypeList = typeof(List<>);
                            var i2 = clonedTypeList.MakeGenericType(innerType);
                            nodeClone = Activator.CreateInstance(i2, null);
                            toClone = false;
                        }
                    }

                    if (toClone) {
                        nodeClone = node.Clone();
                    }

                    object list = null;
                    if (fiListParrent != null) {
                        list = fiListParrent.GetValue(newParrent);
                    }
                    else {
                        list = newParrent;
                    }
                    var mListToArray = list.GetType().GetMethod("Add");
                    mListToArray.Invoke(list, new object[] { nodeClone });
                }
                else {
                    var fiList = node.GetType().GetField("List");
                    if (fiList != null || node is IList) {
                        var argType = fiList.FieldType.GetGenericArguments();
                        var innerType = argType[0];
                        var generic = typeof(List<>);
                        Type lType = null;

                        if (innerType.IsGenericType) {
                            var innerListType = GetTemplateType(innerType);
                            var g2 = typeof(List<>);
                            var i2 = g2.MakeGenericType(innerListType);

                            lType = generic.MakeGenericType(i2);
                        }
                        else {
                            lType = generic.MakeGenericType(innerType);
                        }

                        nodeClone = Activator.CreateInstance(lType, null);

                        var mi = newParrent.GetType().GetMethod(ChildInfo.Setter);
                        mi.Invoke(newParrent, new object[] { nodeClone });
                    }
                    else {
                        nodeClone = node.Clone();

                        var mi = newParrent.GetType().GetMethod(ChildInfo.Setter);
                        mi.Invoke(newParrent, new object[] { nodeClone });
                    }
                }
            }

            // Save state
            _OldParrent.Push(node);
            _NewParrent.Push(nodeClone);

            // And scan
            MoveAllCommentsToNearestStatement(node);
            base.Scan(node);

            // Restore state
            _OldParrent.Pop();
            _NewParrent.Pop();
        }

        private Type CreateListItemType(Type genericType)
        {
            if (genericType.IsGenericType) {
                var generic = typeof(List<>);
                var tType = GetTemplateType(genericType);
                var finalType = CreateListItemType(tType);

                return (finalType == null) ? null : generic.MakeGenericType(finalType);
            }
            else if (typeof(GrammarNode).IsAssignableFrom(genericType)) {
                return genericType;
            }

            return null;
        }

        private Type GetTemplateType(Type template)
        {
            var argType = template.GetGenericArguments();
            return argType[0];
        }

        virtual public bool PostAction(CursorVariableDeclaration node)
        {
            if (node.ForUpdateClause != null) {
                node.ForUpdateClause = null;
                node.AddNote(Note.MODIFIER, Resources.NO_FOR_UPDATE_IN_CURSOR);
            }

            return true;
        }

        virtual public bool PostAction(CreateScalarFunctionStatement node)
        {
            var newNode = _NewParrent.Peek() as CreateProcedureStatement;

            if (newNode == null) {
                //some error occured!!!
                return false;
            }

            if (node.FunctionOption != null) {
                newNode.AddNote(Note.MODIFIER, Resources.NO_FUNCTION_OPTION);
            }

            newNode.AddNote(Note.ERR_MODIFIER, Resources.NO_FUNCTION_RETURN_VALUE);

            return true;
        }

        virtual public bool PostAction(CreateFunctionStatement node)
        {
            var newNode = _NewParrent.Peek() as CreateProcedureStatement;

            if (newNode == null) {
                //some error occured!!!
                return false;
            }

            if (node.FunctionOption != null) {
                newNode.AddNote(Note.MODIFIER, Resources.NO_FUNCTION_OPTION);
            }

            return true;
        }

        virtual public bool PostAction(CreateProcedureStatement node)
        {
            if (node.ForReplication) {
                node.ForReplication = false;
                node.AddNote(Note.MODIFIER, String.Format(Resources.NO_OPTION_FOR_PROCEDURE, "REPLICATION"));
            }

            if (node.Options != null) {
                IList<ProcedureOption> toRemove = [];
                foreach (var opt in node.Options) {
                    var spo = opt as SimpleProcedureOption;

                    if (spo != null) {
                        if (spo.Type == SimpleProcedureOptionType.Encryption) {
                            toRemove.Add(spo);
                            node.AddNote(Note.MODIFIER, String.Format(Resources.NO_OPTION_FOR_PROCEDURE, "ENCRYPTION"));
                        }

                        if (spo.Type == SimpleProcedureOptionType.Recompile) {
                            toRemove.Add(spo);
                            node.AddNote(Note.MODIFIER, String.Format(Resources.NO_OPTION_FOR_PROCEDURE, "RECOMPILE"));
                        }
                    }

                    if (opt is ExecuteAsProcedureOption) {
                        toRemove.Add(opt);
                        node.AddNote(Note.MODIFIER, String.Format(Resources.NO_OPTION_FOR_PROCEDURE, "EXECUTE AS"));
                    }
                }

                foreach (var po in toRemove) {
                    node.Options.Remove(po);
                }
            }

            return true;
        }

        virtual public bool Action(DatepartFunctionExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                var ident = string.Empty;
                var needDateConversion = false;
                switch (exp.Part.Name.ToLowerInvariant()) {
                    case "year":
                    case "yyyy":
                    case "yy":
                        ident = "YEAR";
                        needDateConversion = true;
                        break;
                    case "month":
                    case "mm":
                    case "m":
                        ident = "MONTH";
                        needDateConversion = true;
                        break;
                    case "week":
                    case "wk":
                    case "ww":
                        ident = "WEEK";
                        needDateConversion = true;
                        break;
                    case "minute":
                    case "mi":
                    case "n":
                        ident = "MINUTE";
                        break;
                    case "second":
                    case "ss":
                    case "s":
                        ident = "SECOND";
                        break;
                    case "dw":
                        ident = "WEEKDAY";

                        // These children should be probably skipped for ScanChildren in ReplaceExpression,
                        // but probably its no harm, as they should be already HANA compliant

                        var args = new List<Expression>(new Expression[] { exp.Target });
                        if (args[0] is StringConstantExpression) {
                            args[0] = new DateFormatExpression(args[0] as StringConstantExpression, DateFormatExpressionType.WithSeparator);
                        }
                        var dwExp = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, ident), args);

                        var oneExp = new IntegerConstantExpression(1);

                        return new BinaryAddExpression(dwExp, BinaryAddOperatorType.Plus, oneExp);
                    case "hour":
                    case "hh":
                        ident = "HOUR";
                        break;
                    case "dayofyear":
                    case "dy":
                    case "y":
                        ident = "DAYOFYEAR";
                        needDateConversion = true;
                        break;
                    case "day":
                    case "dd":
                    case "d":
                        ident = "DAYOFMONTH";
                        needDateConversion = true;
                        break;
                    case "isowk":
                    case "isoww":
                        ident = "ISOWEEK";
                        break;
                }
                if (string.IsNullOrEmpty(ident)) {
                    var ns = new HANANotSupportedExpression();
                    ns.AddNote(Note.ERR_MODIFIER, Resources.NO_FUNCTION_DATEPART);
                    return ns;
                }
                else {
                    var args = new List<Expression>(new Expression[] { exp.Target });
                    if (needDateConversion && args[0] is StringConstantExpression) {
                        args[0] = new DateFormatExpression(args[0] as StringConstantExpression, DateFormatExpressionType.WithSeparator);
                    }
                    return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, ident), args);
                }
            };

            return CreateNewExpression(create, exp);
        }
        virtual public bool Action(DatenameFunctionExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                switch (exp.Part.Name.ToUpperInvariant()) {
                    case "YEAR":
                    case "YY":
                    case "YYYY":
                        return new YearFunctionExpression(exp.Target);
                    case "QUARTER":
                    case "QQ":
                    case "Q":
                        return new ToCharFunctionExpression(exp.Target, new Identifier(IdentifierType.Plain, "Q"));
                    case "MONTH":
                    case "MM":
                    case "M":
                        return new MonthFunctionExpression(exp.Target);
                    case "WEEK":
                    case "WK":
                    case "WW":
                        return new WeekFunctionExpression(exp.Target);
                    case "DAY":
                    case "DD":
                    case "D":
                        return new DayOfMonthFunctionExpression(exp.Target);
                    case "DAYOFYEAR":
                    case "DY":
                    case "Y":
                        return new DayOfYearFunctionExpression(exp.Target);
                    case "WEEKDAY":
                    case "DW":
                        return new WeekDayFunctionExpression(exp.Target);
                    case "HOUR":
                    case "HH":
                        return new HourFunctionExpression(exp.Target);
                    case "MINUTE":
                    case "MI":
                    case "N":
                        return new MinuteFunctionExpression(exp.Target);
                    case "SECOND":
                    case "SS":
                    case "S":
                        return new SecondFunctionExpression(exp.Target);
                }

                var ns = new HANANotSupportedExpression();
                ns.AddNote(Note.ERR_MODIFIER, Resources.NO_FUNCTION_DATENAME);
                return ns;
            };

            return CreateNewExpression(create, exp);
        }

        virtual public bool Action(CreateFunctionStatement stmt)
        {
            CreateNewExprDelegate create = delegate {
                GrammarNode gn = new CreateProcedureStatement(stmt.Name, -1, stmt.FunctionParams, null, false, stmt.FunctionBody.Statements);

                gn.AddNote(Note.MODIFIER, Resources.WARN_FUNCTION_CONVERTED_TO_PROC);
                return gn;
            };

            return CreateNewExpression(create, stmt);
        }

        virtual public bool Action(CreateTableValuedFunctionStatement stmt)
        {
            CreateNewExprDelegate create = delegate {
                var lst = new List<Statement> {
                    stmt.QueryStatement
                };

                GrammarNode gn = new CreateProcedureStatement(stmt.Name, -1, stmt.FunctionParams, null, false, lst);

                gn.AddNote(Note.MODIFIER, Resources.WARN_FUNCTION_CONVERTED_TO_PROC);
                return gn;
            };

            return CreateNewExpression(create, stmt);
        }

        //Returns true when node is generic function, but not the specified one
        protected bool ExpressionReturnsDate(Expression exp)
        {
            if (exp is GenericScalarFunctionExpression) {
                if ((exp as GenericScalarFunctionExpression).Name.Name.ToUpper() == "GETDATE" || (exp as GenericScalarFunctionExpression).Name.Name.ToUpper() == "DATEADD") {
                    return true;
                }
            }

            if (exp is DbObjectExpression) {
                //we expect DB object type is Date...
                return true;
            }

            if (exp is CastFunctionExpression) {
                var dType = (exp as CastFunctionExpression).Type;

                if (dType is SimpleBuiltinDataType) {
                    if ((dType as SimpleBuiltinDataType).Type == SimpleBuiltinDataTypeType.Date || (dType as SimpleBuiltinDataType).Type == SimpleBuiltinDataTypeType.DateTime) {
                        return true;
                    }
                }
            }

            if (exp is DateFormatExpression) {
                return true;
            }

            return false;
        }

        virtual protected GrammarNode ActionDateDiff(GenericScalarFunctionExpression exp)
        {
            var str = new Stringifier(config);
            var identDateDiff = string.Empty;
            int div = 1, multi = 1;
            str.Clear();
            (exp.Arguments[0] as DbObjectExpression).Assembly(str);
            var datePartId = str.Statement.ToLowerInvariant().Replace("\"", "");

            switch (datePartId) {
                case "year":
                case "yy":
                case "yyyy":
                    identDateDiff = "YEAR";
                    break;
                case "quarter":
                case "qq":
                case "q":
                    //untranslated
                    break;
                case "month":
                case "mm":
                case "m":
                    //untranslated
                    break;
                case "week":
                case "wk":
                case "ww":
                    //untranslated
                    break;
                case "day":
                case "dd":
                case "d":
                case "dayofyear":
                case "dy":
                case "y":
                case "weekday":
                case "dw":
                case "w":
                    identDateDiff = "DAYS_BETWEEN";
                    break;
                case "hour":
                case "hh":
                    identDateDiff = "SECONDS_BETWEEN";
                    div = 3600;
                    break;
                case "minute":
                case "mi":
                case "n":
                    identDateDiff = "SECONDS_BETWEEN";
                    div = 60;
                    break;
                case "second":
                case "ss":
                case "s":
                    identDateDiff = "SECONDS_BETWEEN";
                    break;
                case "millisecond":
                case "ms":
                    identDateDiff = "NANO100_BETWEEN";
                    div = 10000;
                    break;
                case "microsecond":
                case "mcs":
                    identDateDiff = "NANO100_BETWEEN";
                    div = 10;
                    break;
                case "nanosecond":
                case "ns":
                    identDateDiff = "NANO100_BETWEEN";
                    multi = 100;
                    break;
            }
            if (string.IsNullOrEmpty(identDateDiff)) {
                var ns = new HANANotSupportedExpression();
                ns.AddNote(Note.ERR_MODIFIER, Resources.NO_FUNCTION_DATEDIFF);
                return ns;
            }

            var args = new List<Expression>();
            bool noteFunction = false, noteInteger = false;
            if (exp.Arguments.Count > 1) {
                args.AddRange(exp.Arguments.Skip(1));

                if (args[0] is StringConstantExpression) {
                    args[0] = new DateFormatExpression(args[0] as StringConstantExpression, DateFormatExpressionType.WithSeparator);
                }
                if (args.Count == 2 && args[1] is StringConstantExpression) {
                    args[1] = new DateFormatExpression(args[1] as StringConstantExpression, DateFormatExpressionType.WithSeparator);
                }

                if ((args[0] is IntegerConstantExpression) ||
                    (args.Count > 1 && args[1] is IntegerConstantExpression)) {
                    noteFunction = true;
                }
                if (!ExpressionReturnsDate(args[0])) {
                    args[0] = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "ADD_DAYS"),
                        [new DateTimeConstantExpression(ZERO_TIMESTAMP), args[0]]);
                }
                if (args.Count > 1 && !ExpressionReturnsDate(args[1])) {
                    args[1] = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "ADD_DAYS"),
                        [new DateTimeConstantExpression(ZERO_TIMESTAMP), args[1]]);
                }

                noteInteger = args[0] is IntegerConstantExpression;

                if (args.Count == 2) {
                    noteInteger |= noteInteger |= args[1] is IntegerConstantExpression;
                }
            }

            GrammarNode ret = null;

            if (identDateDiff.Equals("YEAR")) {
                var arg1 = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "YEAR"),
                            [args[0]]);
                var arg2 = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "YEAR"),
                            [args[1]]);

                ret = new BinaryAddExpression(arg2, BinaryAddOperatorType.Minus, arg1);
            }
            else {
                ret = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, identDateDiff), args);
                if (div > 1) {
                    ret = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "FLOOR"),
                        [new MultiplyExpression((Expression) ret, MultiplyOperatorType.Divide, new IntegerConstantExpression(div))]);
                }
                else if (multi > 1) {
                    var hundredExp = new IntegerConstantExpression(multi);
                    ret = new MultiplyExpression(new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, identDateDiff), args), MultiplyOperatorType.Multiply, hundredExp);
                }
            }

            if (noteFunction) {
                ret.AddNote(Note.MODIFIER, Resources.WARN_FUNCTION_AS_ARGUMENT_IN_DATE_FUNCTIONS);
            }
            if (noteInteger) {
                ret.AddNote(Note.MODIFIER, Resources.NO_INTEGER_AS_ARGUMENT_IN_DATE_FUNCTION);
            }
            return ret;
        }

        virtual protected GrammarNode ActionDateAdd(GenericScalarFunctionExpression exp)
        {
            var str = new Stringifier(config);
            var identDateAdd = string.Empty;
            var multi = 1;

            (exp.Arguments[0] as DbObjectExpression).Assembly(str);
            var datePartId = str.Statement.ToLowerInvariant().Replace("\"", "");
            switch (datePartId) {
                case "day":
                case "dd":
                case "d":
                case "dayofyear":
                case "dy":
                case "y":
                case "weekday":
                case "dw":
                case "w":
                    identDateAdd = "ADD_DAYS";
                    break;
                case "month":
                case "mm":
                case "m":
                    identDateAdd = "ADD_MONTHS";
                    break;
                case "week":
                case "ww":
                case "wk":
                    identDateAdd = "ADD_DAYS";
                    multi = 7;
                    break;
                case "quarter":
                case "qq":
                case "q":
                    identDateAdd = "ADD_MONTHS";
                    multi = 3;
                    break;
                case "year":
                case "yy":
                case "yyyy":
                    identDateAdd = "ADD_YEARS";
                    break;
                case "hour":
                case "hh":
                    identDateAdd = "ADD_SECONDS";
                    multi = 3600;
                    break;
                case "minute":
                case "mi":
                case "n":
                    identDateAdd = "ADD_SECONDS";
                    multi = 60;
                    break;
                case "second":
                case "ss":
                case "s":
                    identDateAdd = "ADD_SECONDS";
                    break;
            }
            if (string.IsNullOrEmpty(identDateAdd)) {
                var ns = new HANANotSupportedExpression();
                ns.AddNote(Note.ERR_MODIFIER, Resources.NO_FUNCTION_DATEADD);
                return ns;
            }
            else {
                var args = new List<Expression>();
                bool noteFunction = false, noteInteger = false;
                if (exp.Arguments.Count > 1) {
                    args.AddRange(exp.Arguments.Skip(1).Reverse());

                    if (args[0] is StringConstantExpression) {
                        args[0] = new DateFormatExpression(args[0] as StringConstantExpression, DateFormatExpressionType.WithSeparator);
                    }

                    if (!ExpressionReturnsDate(args[0])) {
                        if (args[0] is IntegerConstantExpression) {
                            noteInteger = true;
                        }

                        args[0] = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "ADD_DAYS"),
                            [new DateTimeConstantExpression(ZERO_TIMESTAMP), args[0]]);
                    }
                }

                if (multi > 1) {
                    if (NeedParensToExpression(args[1])) {
                        args[1] = new MultiplyExpression(new ParensExpression(args[1]), MultiplyOperatorType.Multiply, new IntegerConstantExpression(multi));
                    }
                    else {
                        args[1] = new MultiplyExpression(args[1], MultiplyOperatorType.Multiply, new IntegerConstantExpression(multi));
                    }
                }

                GrammarNode ret = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, identDateAdd), args);
                if (noteFunction) {
                    ret.AddNote(Note.MODIFIER, Resources.WARN_FUNCTION_AS_ARGUMENT_IN_DATE_FUNCTIONS);
                }
                if (noteInteger) {
                    ret.AddNote(Note.MODIFIER, Resources.NO_INTEGER_AS_ARGUMENT_IN_DATE_FUNCTION);
                }
                return ret;
            }
        }

        private bool NeedParensToExpression(Expression exp)
        {
            if (exp is DbObjectExpression || exp is GenericScalarFunctionExpression || exp is IntegerConstantExpression) {
                return false;
            }

            return true;
        }

        virtual public bool Action(GenericScalarFunctionExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                switch (exp.Name.Name.ToUpperInvariant()) {
                    case "SUSER_NAME":
                        return new ParameterlessFunctionExpression(ParameterlessFunctionType.HANACurrentUser);
                    case "GETUTCDATE":
                        return new ParameterlessFunctionExpression(ParameterlessFunctionType.HANACurrentUTCTimeStamp);
                    case "DB_NAME":
                        return new ParameterlessFunctionExpression(ParameterlessFunctionType.HANACurrentSchema);
                    case "LEN":
                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "LENGTH"), exp.Arguments);
                    case "DATEADD":
                        return ActionDateAdd(exp);
                    case "DAY":
                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "DAYOFMONTH"), exp.Arguments);
                    case "DATEDIFF":
                        return ActionDateDiff(exp);
                    case "GETDATE":
                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "NOW"), null);
                    case "LOG":
                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "LN"), exp.Arguments);
                    case "LOG10":
                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "LOG"),
                            [new IntegerConstantExpression(10), exp.Arguments[0]]);
                    case "ISNULL":
                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "IFNULL"), exp.Arguments);
                    case "SPACE":
                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "LPAD"),
                            [new StringConstantExpression(new StringLiteral(StringLiteralType.ASCII, " ")), exp.Arguments[0]]);
                    case "SQUARE":
                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "POWER"),
                            [exp.Arguments[0], new IntegerConstantExpression(2)]);
                    case "PI":
                        return new DecimalConstantExpression(3.14159265358979m);
                    case "RAND": {
                        var ns = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "RAND"), null);
                        ns.AddNote(Note.MODIFIER, Resources.NO_RAND_WITH_SEED);
                        return ns;
                    }
                    case "RADIANS": {
                        // Rad = Deg * PI / 180
                        var ns = new ParensExpression(new MultiplyExpression(exp.Arguments[0], MultiplyOperatorType.Multiply, new DecimalConstantExpression(0.01745329252m)));
                        ns.AddNote(Note.MODIFIER, Resources.NO_RAD_DEG_FUNCTIONS);
                        return ns;
                    }
                    case "DEGREES": {
                        // Deg = Rad * 180 / PI
                        var ns = new ParensExpression(new MultiplyExpression(exp.Arguments[0], MultiplyOperatorType.Multiply, new DecimalConstantExpression(57.295779513083m)));
                        ns.AddNote(Note.MODIFIER, Resources.NO_RAD_DEG_FUNCTIONS);
                        return ns;
                    }
                    case "ATN2":
                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "ATAN2"), exp.Arguments);
                    case "ROUND":
                        var argsRound = new List<Expression>(exp.Arguments);
                        if (argsRound.Count > 2) {
                            argsRound.RemoveRange(2, argsRound.Count - 2);
                        }
                        return new GenericScalarFunctionExpression(exp.Name, argsRound);
                    case "CHARINDEX":
                        var argsLocate = new List<Expression>(exp.Arguments);
                        if (argsLocate.Count > 2) {
                            argsLocate.RemoveRange(2, argsLocate.Count - 2);
                        }

                        //swap order of arguments
                        var tmpExp = argsLocate[0];
                        argsLocate[0] = argsLocate[1];
                        argsLocate[1] = tmpExp;

                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "LOCATE"), argsLocate);
                    case "GROUPING": {
                        var ns = new HANANotSupportedExpression();
                        ns.AddNote(Note.ERR_MODIFIER, Resources.NO_FUNCTION_GROUPING);
                        return ns;
                    }
                    case "EOMONTH": {
                        var args = new List<Expression>();
                        var offset = new IntegerConstantExpression(0);

                        args.Add(new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "ADD_MONTHS"),
                                    [new DateFormatExpression(exp.Arguments[0] as StringConstantExpression, DateFormatExpressionType.WithoutSeparator)]));
                        args.Add(new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "- DAYOFMONTH"),
                                    [ new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "ADD_MONTHS"),
                                        [new DateFormatExpression(exp.Arguments[0] as StringConstantExpression,DateFormatExpressionType.WithoutSeparator)])]));
                        if (exp.Arguments.Count > 1 && exp.Arguments[1] is IntegerConstantExpression) {
                            offset = new IntegerConstantExpression((exp.Arguments[1] as IntegerConstantExpression).Value);
                            offset.Value++;
                        }
                        args[0] = (new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "ADD_MONTHS"),
                                [new DateFormatExpression(exp.Arguments[0] as StringConstantExpression, DateFormatExpressionType.WithoutSeparator), offset]));
                        args[1] = (new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "- DAYOFMONTH"),
                                    [ new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "ADD_MONTHS"),
                                        [new DateFormatExpression(exp.Arguments[0] as StringConstantExpression, DateFormatExpressionType.WithoutSeparator), offset])]));

                        return new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "ADD_DAYS"), args);
                    }
                }
                return null;
            };

            return CreateNewExpression(create, exp);
        }

        virtual public bool Action(CollationExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                var newExp = (Expression) exp.Expression.Clone();
                newExp.AddNote(Note.ERR_MODIFIER, Resources.NO_COLLATE_SPECIFICATION);
                return newExp;
            };

            return CreateNewExpression(create, exp);
        }

        virtual public bool Action(IifFunctionExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                var list = new List<CaseWhenClause> {
                    new CaseWhenClause(exp.BooleanExpression, exp.TrueExpression)
                };
                Expression newExp = new CaseFunctionExpression(null, list, exp.FalseExpression);
                return newExp;
            };
            return CreateNewExpression(create, exp);
        }

        virtual public bool Action(ChooseFunctionExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                var list = new List<CaseWhenClause> {
                    new CaseWhenClause(new ComparisonExpression(exp.IndexExpression, ComparisonOperatorType.Equal, new IntegerConstantExpression(1)), exp.FirstMandatoryOption)
                };
                for (var i = 2; i <= exp.RestOfOptions.Count() + 1; i++) {
                    list.Add(new CaseWhenClause(new ComparisonExpression(exp.IndexExpression, ComparisonOperatorType.Equal, new IntegerConstantExpression(i)), exp.RestOfOptions.ElementAt(i - 2)));
                }
                Expression newExp = new CaseFunctionExpression(null, list, new NullConstantExpression());
                return newExp;
            };
            return CreateNewExpression(create, exp);
        }

        virtual public bool Action(SimpleAggregateFunctionExpression exp)
        {
            switch (exp.Type) {
                case SimpleAggregateFunctionType.StDev:
                    CreateNewExprDelegate createHANAStDev = delegate {
                        var newExp = new SimpleAggregateFunctionExpression(
                            SimpleAggregateFunctionType.HANAStDev, exp.IsDistinct, exp.Target, exp.OverClause
                            );
                        newExp.AddNote(Note.MODIFIER, Resources.MOD_STDEV_TO_STDDEV);
                        return newExp;
                    };

                    return CreateNewExpression(createHANAStDev, exp);
                case SimpleAggregateFunctionType.ChecksumAgg:
                    goto case SimpleAggregateFunctionType.StDevP;
                case SimpleAggregateFunctionType.VarP:
                    goto case SimpleAggregateFunctionType.StDevP;
                case SimpleAggregateFunctionType.StDevP:
                    CreateNewExprDelegate createHANANotSupp = delegate {
                        var funcName = string.Empty;
                        switch (exp.Type) {
                            case SimpleAggregateFunctionType.ChecksumAgg:
                                funcName = "CHECKSUM_AGG";
                                break;
                            case SimpleAggregateFunctionType.StDevP:
                                funcName = "STDEVP";
                                break;
                            case SimpleAggregateFunctionType.VarP:
                                funcName = "VARP";
                                break;
                        }
                        var ns = new HANANotSupportedExpression();
                        ns.AddNote(Note.ERR_MODIFIER, string.Format(Resources.NO_FUNCTION_SIMPLE_AGGR, funcName));
                        return ns;
                    };

                    return CreateNewExpression(createHANANotSupp, exp);
            }

            return Action(exp as Expression);
        }

        virtual public bool Action(BitwiseNotExpression exp)
        {
            return ReplaceWithEmptyNode(exp, "HANANotSupportedExpression", Note.ERR_MODIFIER, Resources.NO_FUNCTION_BITWISENOT);
        }

        virtual public bool Action(ConvertFunctionExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                GrammarNode node = new CastFunctionExpression(exp.Target, exp.Type);
                node.AddNote(Note.MODIFIER, Resources.WARN_NO_FUNCTION_CONVERT);
                return node;
            };

            return CreateNewExpression(create, exp);
        }

        private bool ExpressionReturnsString(Expression exp)
        {
            if (exp is IntegerConstantExpression || exp is DbObjectExpression) {
                return false;
            }

            if (exp is CastFunctionExpression) {
                if ((exp as CastFunctionExpression).Type is StringWithLengthDataType) {
                    var sType = (StringWithLengthDataType) (exp as CastFunctionExpression).Type;

                    if (sType.Type == StringWithLengthDataTypeType.Char || sType.Type == StringWithLengthDataTypeType.NChar ||
                        sType.Type == StringWithLengthDataTypeType.VarChar || sType.Type == StringWithLengthDataTypeType.NVarChar) {
                        return true;
                    }
                }
            }

            if (exp is GenericScalarFunctionExpression) {
                if ((exp as GenericScalarFunctionExpression).ReturnsString()) {
                    return true;
                }
            }

            if (exp is StringConstantExpression) {
                var strExp = (StringConstantExpression) exp;
                var strToTest = strExp.Value.String;

                var match = Regex.Match(strToTest, @"[-+]?\d+");
                if (match.Success && match.Value.Length == strToTest.Trim().Length) {
                    return false;
                }

                return true;
            }

            if (exp is BinaryAddExpression) {
                var bae = (BinaryAddExpression) exp;
                return bae.Operator == BinaryAddOperatorType.Plus && (ExpressionReturnsString(bae.LeftExpression) || ExpressionReturnsString(bae.RightExpression));
            }

            return false;
        }

        /// <summary>
        /// Cover special cases
        /// </summary>
        /// <param name="leftExp"></param>
        /// <param name="rightExp"></param>
        /// <returns>True if both expression should be string-concated, instead of aritmetic plus</returns>
        private bool IsStringConcatExpression(Expression leftExp, Expression rightExp)
        {
            if ((leftExp is StringConstantExpression && rightExp is DbObjectExpression) ||
                (rightExp is StringConstantExpression && leftExp is DbObjectExpression)) {
                return true;
            }

            if (leftExp is StringConstantExpression && rightExp is StringConstantExpression) {
                return true;
            }

            if (ExpressionReturnsString(leftExp) || ExpressionReturnsString(rightExp)) {
                return true;
            }

            return false;
        }

        virtual public bool Action(BinaryAddExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                if (exp.Operator == BinaryAddOperatorType.Plus) {
                    if (IsStringConcatExpression(exp.LeftExpression, exp.RightExpression)) {
                        return new HANAConcatExpression(exp.LeftExpression, exp.RightExpression);
                    }

                }
                return null;
            };

            return CreateNewExpression(create, exp);
        }

        virtual public bool Action(RankingFunctionExpression exp)
        {
            var name = "";
            switch (exp.Type) {
                case RankingFunctionType.Rank:
                    name = "RANK";
                    break;
                case RankingFunctionType.DenseRank:
                    name = "DENSE_RANK";
                    break;
                case RankingFunctionType.RowNumber:
                    name = "ROW_NUMBER";
                    break;
            }
            if (!string.IsNullOrEmpty(name)) {
                CreateNewExprDelegate create = delegate {
                    return new RankingFunctionExpression(exp.Type, exp.OverClause);
                };
            }
            return true;
        }

        virtual public bool Action(NTileFunctionExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                return new NTileFunctionExpression(exp.GroupCount, exp.OverClause);
            };

            return CreateNewExpression(create, exp);
        }

        virtual public bool Action(QuerySpecification qsp)
        {
            if (qsp.FromClause == null) {
                qsp.AddNote(Note.MODIFIER, Resources.WARN_DUMMY_TABLE);

                (_NewParrent.Peek() as QuerySpecification).FromClause =
                [
                    new DbObjectTableSource(new DbObject(new Identifier(IdentifierType.Plain, "DUMMY")), null, null, null),
                ];
            }
            return true;
        }
        virtual public bool Action(ExecStatementSQL exp)
        {
            if (exp.Params != null) {
                exp.AddNote(Note.ERR_MODIFIER, Resources.NO_PARAMS_IN_EXEC);
            }

            if (exp.Context != null) {
                exp.AddNote(Note.ERR_MODIFIER, Resources.NO_AS_CLAUSE);
            }

            if (exp.LinkedServer != null) {
                exp.AddNote(Note.ERR_MODIFIER, Resources.NO_LINKED_SERVER);
            }
            return true;
        }

        virtual public bool Action(DeleteStatement statement)
        {
            var stmt = _NewParrent.Peek() as DeleteStatement;

            if (statement.FromClause != null) {
                stmt.AddNote(Note.ERR_MODIFIER, Resources.NO_FROM_CLAUSE);
            }

            if (statement.TopClause != null) {
                stmt.AddNote(Note.ERR_MODIFIER, Resources.NO_TOP_CLAUSE);
            }

            if (statement.WithClause != null) {
                stmt.AddNote(Note.ERR_MODIFIER, Resources.NO_WITH_CLAUSE);
            }

            if (statement.OptionClause != null) {
                stmt.AddNote(Note.ERR_MODIFIER, Resources.NO_OPTION_CLAUSE);
            }

            stmt.WithClause = null;
            stmt.TopClause = null;
            stmt.FromClause = null;
            stmt.OptionClause = null;
            stmt.OutputClause = null;

            return true;
        }

        virtual public bool Action(DbObjectTableSource tableSource)
        {
            if (tableSource.Hints != null || tableSource.TableSampleClause != null) {
                var newTable = new DbObjectTableSource(tableSource.DbObject, tableSource.Alias, null, null);

                if (tableSource.Hints != null) {
                    newTable.AddNote(Note.MODIFIER, Resources.NO_TABLE_HINTS);
                }

                if (tableSource.TableSampleClause != null) {
                    newTable.AddNote(Note.MODIFIER, Resources.NO_TABLESAMPLE);
                }

                ReplaceNode(newTable);
                return false;
            }

            return true;
        }

        virtual public bool Action(AlterViewStatement node)
        {
            node.AddNote(Note.ERR_MODIFIER, Resources.NO_ALTER_VIEW);

            // not supported, don't bother with childs
            return false;
        }

        virtual public bool Action(DeallocateStatement node)
        {
            var stmt = _NewParrent.Peek() as DeallocateStatement;
            stmt.Hide = true;
            stmt.Terminate = false;
            stmt.AddNote(Note.ERR_MODIFIER, Resources.NO_DEALLOCATE_STATEMENT);

            // not supported, don't bother with childs
            return false;
        }

        virtual public bool Action(MultiplyExpression exp)
        {
            CreateNewExprDelegate create = delegate {
                if (exp.Operator == MultiplyOperatorType.Modulo) {
                    var ns = new GenericScalarFunctionExpression(new Identifier(IdentifierType.Plain, "MOD"),
                            [exp.LeftExpression, exp.RightExpression]);
                    return ns;
                }
                return null;
            };

            return CreateNewExpression(create, exp);
        }

        virtual public bool Action(ValuesClauseDefault exp)
        {
            return ReplaceWithEmptyNode(exp, "HANANotSupportedValuesClause", Note.ERR_MODIFIER, Resources.NO_CLAUSE_DEFAULT_VALUES);
        }

        virtual public bool Action(SelectClause exp)
        {
            if (exp.TopClause != null) {
                if (exp.TopClause.IsPercent) {
                    exp.AddNote(Note.ERR_MODIFIER, Resources.NO_PERCENT);
                    exp.TopClause.IsPercent = false;
                }

                if (exp.TopClause.IsWithTies) {
                    exp.AddNote(Note.ERR_MODIFIER, Resources.NO_TOP_WITH_TIES);
                    exp.TopClause.IsWithTies = false;
                }
            }

            return Action(exp as GrammarNode);
        }

        virtual public bool PostAction(CreateViewStatement node)
        {
            var newNode = (CreateViewStatement) _NewParrent.Peek();

            if (node.CheckOption) {
                newNode.CheckOption = false;
                newNode.AddNote(Note.ERR_MODIFIER, Resources.NO_CHECK_OPTION);
            }

            if (node.Attributes != null) {
                newNode.Attributes = null;
                newNode.AddNote(Note.ERR_MODIFIER, Resources.NO_ATTRIBUTE_IN_CREATE_VIEW);
            }

            newNode.Statement.Terminate = false;

            return false;
        }

        virtual public bool PostAction(AlterViewStatement node)
        {
            (_NewParrent.Peek() as Statement).Hide = true;
            return false;
        }

        virtual public bool PostAction(DropViewStatement node)
        {
            if (node.Views.Count > 1) {
                var newNode = (DropViewStatement) _NewParrent.Peek();
                var views = newNode.Views;

                //split one drop to several drop statements
                var statement = GetNearestFather();
                foreach (var view in views) {
                    var singleDrop = new DropViewStatement([view]);
                    if (view == views.Last()) {
                        singleDrop.AddNote(Note.MODIFIER, Resources.WARN_DROP_VIEW_DIVIDED);
                    }
                    statement.AddStatement(singleDrop);
                }
                statement.RemoveStatement(newNode);
                return true;
            }

            return false;
        }


        virtual public bool PostAction(DropIndexStatement node)
        {
            var newNode = (DropIndexStatement) _NewParrent.Peek();
            var actions = newNode.Actions;

            if (actions.Count > 1) {
                //split insert into several insert statemnts
                var statement = GetNearestFather();
                foreach (var action in actions) {
                    var singleDrop = new DropIndexStatement([action]);
                    if (action == actions.Last()) {
                        singleDrop.AddNote(Note.MODIFIER, Resources.WARN_DROP_INDEX_DIVIDED);
                        if (GetNearestProcedure() != null) {
                            singleDrop.AddNote(Note.MODIFIER, Resources.WARN_DROP_INDEX_IN_PROC);
                        }
                    }

                    statement.AddStatement(singleDrop);

                    if (action.Options != null) {
                        action.Options = null;
                        singleDrop.AddNote(Note.ERR_MODIFIER, Resources.NO_WITH_OPTIONS_FOR_DROP_INDEX);
                    }

                    if (action.TableSource != null) {
                        action.TableSource = null;
                        singleDrop.AddNote(Note.ERR_MODIFIER, Resources.NO_TABLE_FOR_INDEX);
                    }
                }
                statement.RemoveStatement(_NewParrent.Peek() as Statement);
                return true;
            }
            else {
                if (GetNearestProcedure() != null) {
                    newNode.AddNote(Note.MODIFIER, Resources.WARN_DROP_INDEX_IN_PROC);
                }
            }

            return false;
        }

        virtual public bool PostAction(GoStatement stmt)
        {
            (_NewParrent.Peek() as Statement).Hide = true;
            return false;
        }

        virtual public bool PostAction(AlterIndexStatement node)
        {
            var newNode = (AlterIndexStatement) _NewParrent.Peek();

            if (node.TableSource != null) {
                newNode.TableSource = null;
                newNode.AddNote(Note.ERR_MODIFIER, Resources.NO_TABLE_FOR_INDEX);
            }

            if (node.Index.IsEmpty) {
                newNode.AddNote(Note.ERR_MODIFIER, Resources.NO_ALL_INDEXES);
            }

            if (node.Action is DisableAlterIndexAction) {
                newNode.Action = null;
                newNode.AddNote(Note.ERR_MODIFIER, String.Format(Resources.ONLY_REBUILD_IN_ALTER_INDEX, "DISABLE"));
            }

            if (node.Action is ReorganizeAlterIndexAction) {
                newNode.Action = null;
                newNode.AddNote(Note.ERR_MODIFIER, String.Format(Resources.ONLY_REBUILD_IN_ALTER_INDEX, "REORGANIZE"));
            }

            if (node.Action is SetAlterIndexAction) {
                newNode.Action = null;
                newNode.AddNote(Note.ERR_MODIFIER, String.Format(Resources.ONLY_REBUILD_IN_ALTER_INDEX, "SET"));
            }

            if (node.Action is RebuildAlterIndexAction) {
                var rebuild = (RebuildAlterIndexAction) newNode.Action;

                if (rebuild.Options != null) {
                    rebuild.AddNote(Note.ERR_MODIFIER, String.Format(Resources.NO_OPTION_ALLOWED_IN_REBUILD, "WITH"));
                    rebuild.Options = null;
                }

                if (rebuild.Partition != null) {
                    rebuild.AddNote(Note.ERR_MODIFIER, String.Format(Resources.NO_OPTION_ALLOWED_IN_REBUILD, "PARTITION"));
                    rebuild.Options = null;
                }
            }

            return false;
        }

        virtual public bool PostAction(UpdateStatement statement)
        {
            var ret = _NewParrent.Peek() as UpdateStatement;

            if (statement.TopClause != null) {
                ret.AddNote(Note.ERR_MODIFIER, Resources.NO_TOP_CLAUSE);
                if (statement.TopClause.IsPercent) {
                    ret.AddNote(Note.ERR_MODIFIER, Resources.NO_PERCENT);
                }
                ret.TopClause = null;
            }

            if (statement.OptionClause != null) {
                ret.AddNote(Note.ERR_MODIFIER, Resources.NO_OPTION_CLAUSE);
                ret.OptionClause = null;
            }

            if (statement.OutputClause != null) {
                ret.AddNote(Note.ERR_MODIFIER, Resources.NO_OUTPUT_CLAUSE);
                ret.OutputClause = null;
            }

            return false;
        }

        virtual public bool PostAction(DropTableStatement node)
        {
            //divide statement into multiple statements
            var newNode = (DropTableStatement) _NewParrent.Peek();
            var tableSources = (List<DbObjectTableSource>) newNode.TableSources;

            if (tableSources.Count > 1) {
                //split insert into several insert statemnts
                var statement = GetNearestFather();
                foreach (var tableSource in tableSources) {
                    var rowStatement = new DropTableStatement([(DbObjectTableSource) tableSource.Clone()]);
                    if (tableSource == tableSources.Last()) {
                        rowStatement.AddNote(Note.MODIFIER, Resources.WARN_DROP_DIVIDED);
                    }
                    statement.AddStatement(rowStatement);
                }

                return false;
            }

            return false;
        }

        virtual public bool PostAction(SelectStatement stmt)
        {
            if ((stmt.QueryExpression is QuerySpecification) &&
                ((stmt.QueryExpression as QuerySpecification).IntoClause != null)) {
                var newStmt = (_NewParrent.Peek() as SelectStatement);

                var statement = GetNearestFather();
                var qsp = (newStmt.QueryExpression as QuerySpecification);
                var create = new CreateTableStatement(qsp.IntoClause, false, null, null, null, null, null, qsp);
                statement.AddStatement(create);

                // remove old statement
                statement.RemoveStatement(newStmt);
                return false;
            }
            if (stmt.QueryExpression is QuerySpecification) {
                if ((stmt.QueryExpression as QuerySpecification).SelectClause.SelectItems.Where(s => s is SelectVariableItem).Count() > 0) {
                    var father = GetNearestFather();
                    var oStmt = _NewParrent.Peek() as SelectStatement;
                    var list = new List<SelectVariableItem>();
                    foreach (var itm in (oStmt.QueryExpression as QuerySpecification).SelectClause.SelectItems.Where(s => s is SelectVariableItem)) {
                        list.Add(itm as SelectVariableItem);
                    }
                    SelectVariableStatement nStmt;
                    if (stmt.QueryExpression is QuerySpecification) {
                        nStmt = new SelectVariableStatement(list, (oStmt.QueryExpression as QuerySpecification).FromClause);
                    }
                    else {
                        nStmt = new SelectVariableStatement(list);
                    }

                    //remove VariableItems
                    foreach (var itm in list) {
                        (oStmt.QueryExpression as QuerySpecification).SelectClause.SelectItems.Remove(itm);
                    }

                    //if items list is empty remopve old statement
                    if ((oStmt.QueryExpression as QuerySpecification).SelectClause.SelectItems.Count() == 0) {
                        father.ReplaceStatement(oStmt, nStmt);
                    }
                    else {
                        father.AddStatement(nStmt);
                        father.RemoveStatement(oStmt);
                    }
                }
            }

            return false;
        }

        virtual public bool PostAction(ValuesClauseSelect cls)
        {
            (_NewParrent.Peek() as ValuesClauseSelect).Statement.Terminate = false;
            return false;
        }

        virtual public bool PostAction(SqlStartStatement stmt)
        {
            //CreateAlterProcedureStatement proc = GetNearestProcedure();
            (_NewParrent.Peek() as SqlStartStatement).Hide = true;
            return false;
        }

        virtual public bool PostAction(CreateBaseTypeStatement stmt)
        {
            (_NewParrent.Peek() as CreateBaseTypeStatement).Hide = true;
            return false;
        }

        virtual public bool Action(UseStatement stmt)
        {
            var ret = new HANASetSchemaStatement(stmt.Database);
            GetNearestFather().ReplaceStatement(_NewParrent.Peek() as Statement, ret);
            return true;
        }

        virtual public bool PostAction(TriggerAction act)
        {
            var toRemove = new List<Statement>();
            var nAct = _NewParrent.Peek() as TriggerAction;
            foreach (var stmt in nAct.Statements.Statements) {
                if (stmt is SelectStatement) {
                    toRemove.Add(stmt);
                }
            }
            foreach (var stmt in toRemove) {
                Statement nStmt = new HANANotSupportSimpleStatementinTriggers();
                nStmt.AddNote(Note.MODIFIER, Resources.NO_SIMPLE_STATEMENTS_IN_TRIGGERS);
                nAct.Statements.ReplaceStatement(stmt, nStmt);
            }

            return false;
        }

        virtual public bool Action(WithCommonTable exp)
        {
            CreateNewExprDelegate create = delegate {
                if (exp.Query != null) {
                    Action(exp.Query);
                }
                if (exp.Name != null) {
                    exp.Name.Type = IdentifierType.Quoted;
                }
                return exp;
            };

            return CreateNewExpression(create, exp);
        }

        virtual public bool Action(InsertStatement statement)
        {
            var ret = _NewParrent.Peek() as InsertStatement;

            if (statement.TopClause != null) {
                ret.AddNote(Note.ERR_MODIFIER, Resources.NO_TOP_CLAUSE);
                ret.TopClause = null;
            }

            if (statement.OutputClause != null) {
                ret.AddNote(Note.ERR_MODIFIER, Resources.NO_OUTPUT_CLAUSE);
                ret.OutputClause = null;
            }

            if (statement.ValuesClause is ValuesClauseExec) {
                var valuesclause = (ValuesClauseExec) statement.ValuesClause;
                if (valuesclause.ExecStatement is ExecStatementSP) {
                    ret.InsertTarget.AddNote(Note.ERR_MODIFIER, Resources.NO_EXEC_SP_IN_INSERT);
                    (ret.ValuesClause as ValuesClauseExec).ExecStatement.Terminate = false;
                }
                else if (valuesclause.ExecStatement is ExecStatement) {
                    ret.InsertTarget.AddNote(Note.ERR_MODIFIER, Resources.NO_EXEC_IN_INSERT);
                    (ret.ValuesClause as ValuesClauseExec).ExecStatement.Terminate = false;
                }
            }

            return true;
        }

        virtual public bool PostAction(InsertStatement node)
        {
            //divide statement into multiple statements
            if (node.ValuesClause is ValuesClauseValues) {
                var newNode = (InsertStatement) _NewParrent.Peek();
                var values = (ValuesClauseValues) newNode.ValuesClause;

                if (values.Values.Count > 1) {
                    //split insert into several insert statemnts
                    var statement = GetNearestFather();
                    foreach (var row in values.Values) {
                        var rowStatement = new InsertStatement(newNode.TopClause, newNode.InsertTarget, newNode.ColumnList, newNode.OutputClause, null);
                        rowStatement.ValuesClause = new ValuesClauseValues(row);
                        if (row == values.Values.Last()) {
                            rowStatement.AddNote(Note.MODIFIER, Resources.WARN_INSERT_DIVIDED);
                        }
                        statement.AddStatement(rowStatement);
                    }
                    // Remove orginal statement
                    statement.RemoveStatement(newNode);
                    return false;
                }
            }

            return false;
        }

        virtual public bool PostAction(CreateTableStatement stmto)
        {
            var stmt = _NewParrent.Peek() as CreateTableStatement;
            var toRemove = new List<CreateTableDefinition>();
            if (stmt.Definitions != null) {
                foreach (var def in stmt.Definitions) {
                    if (def is PrimaryKeyTableConstraint) {
                        var father = GetNearestFather();
                        var nStmt = new AlterTableStatement(new DbObjectTableSource(stmt.Name, null, null, null),
                            new AddAlterTableAction(null,
                            [
                                def
                            ]));

                        // Can do that now, because there is no replacement done in Action
                        Action(nStmt.Action as AddAlterTableAction);

                        father.AddStatement(nStmt);
                        toRemove.Add(def);
                    }
                }
                foreach (var def in toRemove) {
                    stmt.Definitions.Remove(def);
                }
            }
            return true;
        }

        virtual public bool PostAction(BeginTransactionStatement stmt)
        {
            (_NewParrent.Peek() as Statement).Hide = true;

            return false;
        }

        virtual public bool PreAction(WaitForStatement stmt)
        {
            (_NewParrent.Peek() as Statement).Hide = true;

            return false;
        }

        virtual public bool PreAction(TryStatement stmt)
        {
            var proc = GetNearestProcedure();
            if (proc != null) {
                GetNearestFather().RemoveStatement(_NewParrent.Peek() as Statement);
                proc.Statements.AddStatement(_NewParrent.Peek() as Statement);
            }

            (_NewParrent.Peek() as Statement).Hide = true;
            return true;
        }

        virtual public bool PostAction(ThrowStatement stmt)
        {
            (_NewParrent.Peek() as Statement).Hide = true;

            return false;
        }

        virtual public bool PostAction(GotoStatement stmt)
        {
            (_NewParrent.Peek() as Statement).Hide = true;

            return false;
        }

        virtual public bool PostAction(LabelStatement stmt)
        {
            (_NewParrent.Peek() as Statement).Hide = true;

            return false;
        }

        virtual public bool PostAction(DeclareStatement stmt)
        {
            BlockStatement statement;
            var proc = GetNearestProcedure();
            if (proc != null) {
                statement = proc.Declarations;
                proc.Declarations.AddStatement(_NewParrent.Peek() as Statement);
                if (GetNearestFather() == proc.Statements) {
                    proc.Statements.RemoveStatement(_NewParrent.Peek() as Statement);
                }
                else {
                    GetNearestFather().RemoveStatement(_NewParrent.Peek() as Statement);
                }
            }
            else {
                statement = GetNearestFather();
            }

            if (stmt.Declarations.Count > 1) {
                //split insert into several insert statemnts
                foreach (var decl in stmt.Declarations) {
                    if (decl is TableVariableDeclaration) {
                        var dec = decl as TableVariableDeclaration;
                        var stmtCT = new CreateTableTypeStatement(new DbObject(new Identifier(IdentifierType.Plain, dec.Variable.Value + "_TYPE")), dec.Definition);
                        statement.AddStatement(stmtCT);
                    }
                    var list = new List<VariableDeclaration> {
                        decl
                    };
                    var nStmt = new DeclareStatement(list);
                    statement.AddStatement(nStmt);
                }
                statement.RemoveStatement(_NewParrent.Peek() as Statement);
            }
            else if (stmt.Declarations[0] is TableVariableDeclaration) {
                var dec = stmt.Declarations[0] as TableVariableDeclaration;
                var stmtCT = new CreateTableTypeStatement(new DbObject(new Identifier(IdentifierType.Plain, dec.Variable.Value + "_TYPE")), dec.Definition);
                statement.InsertBefore(_NewParrent.Peek() as Statement, stmtCT);
            }
            else if (stmt.Declarations[0] is CursorVariableDeclaration) {
                ((_NewParrent.Peek() as DeclareStatement).Declarations[0] as CursorVariableDeclaration).Statement.Terminate = false;
            }
            return false;
        }

        virtual public bool PreAction(TableVariableDeclaration dec)
        {
            dec.Variable.IsArgument = false;
            return false;
        }

        virtual public bool Action(SetDateFirstVariableStatement stmt)
        {
            return ReplaceWithEmptyNode(stmt, "HANANotSupportedStatement", Note.ERR_MODIFIER, Resources.NO_SET_DATEFIRST);
        }

        virtual public bool Action(SetDateFirstStatement stmt)
        {
            return ReplaceWithEmptyNode(stmt, "HANANotSupportedStatement", Note.ERR_MODIFIER, Resources.NO_SET_DATEFIRST);
        }

        virtual public bool Action(SetDateFormatStatement stmt)
        {
            return ReplaceWithEmptyNode(stmt, "HANANotSupportedStatement", Note.ERR_MODIFIER, Resources.NO_SET_DATEFORMAT);
        }

        virtual public bool Action(SetDateFormatVariableStatement stmt)
        {
            return ReplaceWithEmptyNode(stmt, "HANANotSupportedStatement", Note.ERR_MODIFIER, Resources.NO_SET_DATEFORMAT);
        }

        virtual public bool Action(SetLockTimeoutStatement stmt)
        {
            return ReplaceWithEmptyNode(stmt, "HANANotSupportedStatement", Note.ERR_MODIFIER, String.Format(Resources.NO_SETTING_SUPPORTED, "LOCK TIMEOUT"));
        }

        virtual public bool Action(SetSpecialStatement stmt)
        {
            var settingName = stmt.Type.ToString();
            var msg = Resources.NO_TSQL_SETTINGS_SUPPORTED;

            if (stmt.Type >= SetOptionType.ANSI_WARNINGS && stmt.Type <= SetOptionType.XACT_ABORT) {
                msg = String.Format(Resources.NO_SETTING_SUPPORTED, stmt.Type.ToString());
            }

            return ReplaceWithEmptyNode(stmt, "HANANotSupportedStatement", Note.ERR_MODIFIER, msg);
        }

        virtual public bool Action(SetIdentityInsertStatement stmt)
        {
            return ReplaceWithEmptyNode(stmt, "HANANotSupportedStatement", Note.ERR_MODIFIER, String.Format(Resources.NO_SETTING_SUPPORTED, "IDENTITY INSERT"));
        }

        virtual public bool Action(UpdateStatisticStatement stmt)
        {
            return ReplaceWithEmptyNode(stmt, "HANANotSupportedStatement", Note.ERR_MODIFIER, Resources.NO_UPDATE_STATISTICS);
        }

        virtual public bool PreAction(SetStatement stmt)
        {
            if (stmt.Variable != null)
                stmt.Variable.IsArgument = false;

            var proc = GetNearestProcedure();

            if (proc != null && stmt.Variable != null) {
                foreach (var param in proc.Parameters) {
                    if (param is DataTypeProcedureParameter) {
                        if ((param as DataTypeProcedureParameter).Name == stmt.Variable.Value) {
                            (param as DataTypeProcedureParameter).InOut = true;
                        }
                    }
                }
            }
            return false;
        }


        virtual public bool PostAction(SetStatement stmt)
        {
            if (stmt.Expression is SubqueryExpression) {
                var nStmt = new SelectVariableStatement([new SelectVariableItem(stmt.Variable, stmt.Operator, stmt.Expression)]);
                GetNearestFather().ReplaceStatement(_NewParrent.Peek() as Statement, nStmt);
            }

            return false;
        }

        virtual public bool PreAction(ScalarVariableDeclaration decl)
        {
            if (decl.Variable != null)
                decl.Variable.IsArgument = false;

            return false;
        }

        virtual public bool PreAction(SelectVariableItem item)
        {
            if (item.Variable != null)
                item.Variable.IsArgument = false;

            return false;
        }

        virtual public bool PreAction(IfStatement stmt)
        {
            var proc = GetNearestProcedure();

            if (stmt.Condition is ComparisonExpression) {
                if ((stmt.Condition as ComparisonExpression).LeftExpression is SubqueryExpression) {
                    var statement = GetNearestFather();
                    var variableName = VarPool.GetNewVariableName();
                    var vExp = new VariableExpression(variableName);
                    vExp.IsArgument = false;
                    var dec = new ScalarVariableDeclaration(vExp, new SimpleBuiltinDataType(SimpleBuiltinDataTypeType.Int), null);
                    var decStmt = new DeclareStatement([dec as VariableDeclaration]);
                    if (proc != null) {
                        proc.Declarations.AddStatement(decStmt);
                    }
                    else {
                        statement.InsertBefore((_NewParrent.Peek() as Statement), decStmt);
                    }
                    var vExp2 = new VariableExpression(variableName);
                    var item = new SelectVariableItem(vExp, AssignmentType.Assign, (stmt.Condition as ComparisonExpression).LeftExpression);
                    var sStmt = new SelectVariableStatement([item]);
                    statement.InsertBefore((_NewParrent.Peek() as Statement), sStmt);
                    (stmt.Condition as ComparisonExpression).LeftExpression = vExp2;
                }
            }
            else if (stmt.Condition is ExistsExpression) {
                var variableName = VarPool.GetNewVariableName();
                var var1 = new VariableExpression(variableName);
                var1.IsArgument = false;
                var decStmt = new DeclareStatement([new ScalarVariableDeclaration(var1, new SimpleBuiltinDataType(SimpleBuiltinDataTypeType.Int), null)]);
                if (proc != null) {
                    proc.Declarations.AddStatement(decStmt);
                }
                else {
                    GetNearestFather().InsertBefore((_NewParrent.Peek() as Statement), decStmt);
                }
                var select = new SelectStatement((stmt.Condition as ExistsExpression).Query, null, ForClauseType.None, null);
                var sub = new SubqueryExpression(select);
                var nStmt = new SelectVariableStatement([new SelectVariableItem(var1, AssignmentType.Assign, sub)]);
                GetNearestFather().InsertBefore(_NewParrent.Peek() as Statement, nStmt);
                var var2 = new VariableExpression(variableName);
                stmt.Condition = new ComparisonExpression(var2, ComparisonOperatorType.GreaterThan, new IntegerConstantExpression(0));
            }
            else if (stmt.Condition is IsNullExpression) {
                var variableName = VarPool.GetNewVariableName();
                var var1 = new VariableExpression(variableName);
                var1.IsArgument = false;
                var decStmt = new DeclareStatement([new ScalarVariableDeclaration(var1, new SimpleBuiltinDataType(SimpleBuiltinDataTypeType.Int), null)]);
                if (proc != null) {
                    proc.Declarations.AddStatement(decStmt);
                }
                else {
                    GetNearestFather().InsertBefore((_NewParrent.Peek() as Statement), decStmt);
                }
                var nStmt = new SelectVariableStatement([new SelectVariableItem(var1, AssignmentType.Assign, (stmt.Condition as IsNullExpression).Target)]);
                GetNearestFather().InsertBefore(_NewParrent.Peek() as Statement, nStmt);
                var var2 = new VariableExpression(variableName);
                (stmt.Condition as IsNullExpression).Target = var2;
            }
            return false;
        }

        virtual public bool PostAction(AlterTableStatement stmt)
        {
            if (stmt.Action is AddAlterTableAction) {
                var alterStmt = (_NewParrent.Peek() as AlterTableStatement);
                var act = (alterStmt.Action as AddAlterTableAction);
                if (act.Definitions.Count > 1) {
                    var toRemove = new List<CreateTableDefinition>();
                    foreach (var def in act.Definitions) {
                        if (def is PrimaryKeyTableConstraint) {
                            var father = GetNearestFather();
                            var nStmt = new AlterTableStatement(alterStmt.TableSource,
                                new AddAlterTableAction(act.WithCheck, [def]));

                            // Can do that now, because there is no replacement done in Action
                            Action(nStmt.Action as AddAlterTableAction);

                            father.AddStatement(nStmt);
                            toRemove.Add(def);
                        }
                    }
                    foreach (var def in toRemove) {
                        (alterStmt.Action as AddAlterTableAction).Definitions.Remove(def);
                    }
                }
            }
            if (stmt.Action is DropAlterTableAction) {
                var alterStmt = (_NewParrent.Peek() as AlterTableStatement);
                var act = (alterStmt.Action as DropAlterTableAction);
                if (act.Definitions.Count > 1) {
                    var toRemove = new List<DropAlterTableDefinition>();
                    foreach (var def in act.Definitions) {
                        if (def is DropConstraintAlterTableDefinition) {
                            var father = GetNearestFather();
                            Statement nStmt = new AlterTableStatement(alterStmt.TableSource,
                                new DropAlterTableAction([def]));
                            father.AddStatement(nStmt);
                            toRemove.Add(def);
                        }
                    }

                    foreach (var def in toRemove) {
                        (alterStmt.Action as DropAlterTableAction).Definitions.Remove(def);
                    }
                }
            }
            return false;
        }

        virtual public bool Action(OrderedColumn col)
        {
            if (col.Direction != OrderDirection.Nothing) {
                foreach (var o in _OldParrent) {
                    if (o is PrimaryKeyTableConstraint) {
                        CreateNewExprDelegate create = delegate {
                            var newCol = new OrderedColumn(col.Name, OrderDirection.Nothing);
                            newCol.AddNote(Note.MODIFIER, Resources.NO_DESC_FOR_INDEX);
                            return newCol;
                        };

                        return CreateNewExpression(create, col);
                    }
                }
            }
            return true;
        }

        virtual public bool PostAction(TableConstraint constraint)
        {
            var var = _NewParrent.Peek() as PrimaryKeyTableConstraint;
            if (var != null) {
                if (var.IndexOptions != null) {
                    var.AddNote(Note.MODIFIER, String.Format(Resources.NO_PRIMARY_KEY_TABLE_CONSTRAINT, "WITH clause"));
                    var.IndexOptions = null;
                }
                if (var.OnClause != null) {
                    var.AddNote(Note.MODIFIER, String.Format(Resources.NO_PRIMARY_KEY_TABLE_CONSTRAINT, "ON clause"));
                    var.OnClause = null;
                }
            }
            return true;
        }

        virtual public bool Action(RecompileExecOption execopt)
        {
            return ReplaceWithHANANotSupportedExecStatementSP(execopt, Resources.NO_RECOMPILE_ON_HANA);
        }

        virtual public bool Action(OutputClause outcls)
        {
            return ReplaceWithHANANotSupportedOuputClause(outcls, Resources.NO_OUTPUT_CLAUSE);
        }

        virtual public bool Action(AlterProcedureStatement altprocstmt)
        {
            return ReplaceWithHANANotSupportedAlterProcedureStatement(altprocstmt, Resources.NO_ALTER_STATEMENT);
        }

        virtual public bool Action(AlterColumnAddDropAlterTableAction exp)
        {
            return ReplaceWithHANANotSupportedAlterTableAction(exp, Resources.NO_COLUMN_CONSTRAINTS);
        }

        virtual public bool Action(CheckAlterTableAction exp)
        {
            return ReplaceWithHANANotSupportedAlterTableAction(exp, Resources.NO_CHECK_CLAUSE);
        }

        virtual public bool Action(TriggerAlterTableAction exp)
        {
            return ReplaceWithHANANotSupportedAlterTableAction(exp, Resources.NO_TRIGGER_CLAUSE);
        }

        virtual public bool Action(ChangeTrackingAlterTableAction exp)
        {
            return ReplaceWithHANANotSupportedAlterTableAction(exp, Resources.NO_CHANGE_TRACKING_CLAUSE);
        }

        virtual public bool Action(SwitchPartitionAlterTableAction exp)
        {
            return ReplaceWithHANANotSupportedAlterTableAction(exp, Resources.NO_SWITH_CLAUSE);
        }

        virtual public bool Action(SetFilestreamAlterTableAction exp)
        {
            return ReplaceWithHANANotSupportedAlterTableAction(exp, Resources.NO_SET_CLAUSE);
        }

        virtual public bool Action(RebuildPartitionAlterTableAction exp)
        {
            return ReplaceWithHANANotSupportedAlterTableAction(exp, Resources.NO_REBUILD_CLAUSE);
        }

        virtual public bool Action(LockEscalationTableOptionAlterTableAction exp)
        {
            return ReplaceWithHANANotSupportedAlterTableAction(exp, Resources.NO_LOCK_ESCALATION_CLAUSE);
        }

        virtual public bool Action(FiletableTableOptionAlterTableAction exp)
        {
            return ReplaceWithHANANotSupportedAlterTableAction(exp, Resources.NO_FILETABLE_CLAUSE);
        }

        private bool ReplaceWithHANANotSupportedAlterTableAction(AlterTableAction exp, string note)
        {
            return ReplaceWithEmptyNode(exp, "HANANotSupportedAlterTableAction", Note.MODIFIER, note);
        }

        private bool ReplaceWithHANANotSupportedOuputClause(OutputClause outcls, string note)
        {
            return ReplaceWithEmptyNode(outcls, "HANANotSupportedOutputClause", Note.MODIFIER, note);
        }

        private bool ReplaceWithHANANotSupportedExecStatementSP(RecompileExecOption execopt, string note)
        {
            return ReplaceWithEmptyNode(execopt, "HANANotSupportedExecStatementSP", Note.MODIFIER, note);
        }

        private bool ReplaceWithHANANotSupportedAlterProcedureStatement(AlterProcedureStatement alterprocstmt, string note)
        {
            return ReplaceWithEmptyNode(alterprocstmt, "HANANotSupportedAlterProcedureStatement", Note.MODIFIER, note);
        }

        virtual public bool Action(CursorSource source)
        {
            if (source.VarName != null) {
                (_NewParrent.Peek() as CursorSource).Name = new Identifier(IdentifierType.Plain, source.VarName.Value);
                (_NewParrent.Peek() as CursorSource).VarName = null;
                (_NewParrent.Peek() as CursorSource).AddNote(Note.MODIFIER, Resources.NO_CURSOR_AS_VARIABLE);
            }
            return false;
        }

        virtual public bool Action(AlterColumnDefineAlterTableAction exp)
        {
            if (exp.Collation != null) {
                exp.AddNote(Note.MODIFIER, String.Format(Resources.NO_COLUMN_CONSTRAINT, "COLLATION"));
                exp.Collation = null;
            }
            if (exp.IsSparse) {
                exp.AddNote(Note.MODIFIER, String.Format(Resources.NO_COLUMN_CONSTRAINT, "SPARSE"));
                exp.IsSparse = false;
            }
            return Action(exp as AlterTableAction);
        }

        virtual public bool Action(AddAlterTableAction exp)
        {
            if (exp.WithCheck != null) {
                exp.AddNote(Note.MODIFIER, Resources.NO_WITH_CHECK_FOR_ALTER_TABLE);
                exp.WithCheck = null;
            }

            return Action(exp as AlterTableAction);
        }

        private bool ReplaceWithEmptyNode(GrammarNode oldNode, string newNodeType, string noteType, string note)
        {
            var newNode = (GrammarNode) System.Activator.CreateInstance(Type.GetType("Translator." + newNodeType));
            newNode.AddNote(noteType, note);

            ReplaceNode(newNode);
            return false;
        }

        private bool CreateNewExpression(CreateNewExprDelegate create, GrammarNode oldExp)
        {
            var newExpr = create();
            if (newExpr != null) {
                ReplaceNode(newExpr);
                return false;
            }
            else {
                Action(oldExp as Expression);
                return true;
            }
        }

        private void ReplaceNode(GrammarNode newExpr)
        {
            if (_NewParrent.Count >= 2) {
                // Our new parrent is 2 pops away
                var currParrent = _NewParrent.Count > 0 ? _NewParrent.Pop() : null;
                var newParrent = _NewParrent.Peek();

                // Replace expression
                if (newParrent is IList) {
                    // If list, old node must be replaced
                    var list = newParrent as IList;
                    var idx = list.IndexOf(currParrent);

                    //list[ChildInfo.Index] = newExpr;
                    list[idx] = newExpr;
                }
                else {
                    var mi = newParrent.GetType().GetMethod(ChildInfo.Setter);
                    mi.Invoke(newParrent, new object[] { newExpr });
                }

                // Save replaced expression
                newExpr.ReplacedNode = currParrent as GrammarNode;

                // Push new children to stack
                _NewParrent.Push(newExpr);

                // Scan only children (they are shared with old expression right now)
                ScanChildren(newExpr);
            }
        }
    }
}
