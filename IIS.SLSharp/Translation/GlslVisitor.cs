﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;
using IIS.SLSharp.Shaders;
using Mono.Cecil;

namespace IIS.SLSharp.Translation
{
    internal sealed partial class GlslVisitor : IAstVisitor<int, StringBuilder>
    {
        private readonly HashSet<Tuple<string, string>> _functions = new HashSet<Tuple<string, string>>();

        private readonly CustomAttribute _attr;

        public IEnumerable<Tuple<string, string>> Functions
        {
            get { return _functions; }
        }

        private static void ValidateComplexType(TypeReference type)
        {
            var typeDef = type.Resolve();
            var declType = typeDef.DeclaringType;

            if (declType != null && declType.MetadataToken.ToInt32() == typeof(ShaderDefinition).MetadataToken)
                return;

            throw new SLSharpException(type.FullName + " is invalid in a shader program.");
        }

        public static void ValidateType(TypeReference t)
        {
            // ToGlslType returns normally if it's a correct type
            ToGlslType(t);
        }

        public static string ToGlslType(TypeReference t)
        {
            switch (t.FullName)
            {
                case "System.Single":
                    return "float";
                case "System.Double":
                    return "double";
                case "System.Void":
                    return "void";
                case "System.Int32":
                    return "int";
                case "System.UInt32":
                    return "uint";
                case "System.Boolean":
                    return "bool";
                default:
                    ValidateComplexType(t);
                    return t.Name;
            }
        }

        public static string ToGlslParamType(ParameterDefinition p)
        {
            var t = ToGlslType(p.ParameterType.Resolve());
            if (p.ParameterType.IsByReference)
            {
                if (p.IsOut)
                    return "out " + t;
                return "inout " + t;
            }
            return t;
        }

        public string Result { get; private set; }

        public static string GetParameterString(MethodDefinition m)
        {
            var sig = string.Empty;

            var parameters = m.Parameters;
            if (parameters.Count > 0)
            {
                sig = parameters.Take(parameters.Count - 1).Aggregate(sig, (current, v) =>
                    current + ToGlslParamType(v) + " " + v.Name + ", ");

                var l = parameters.Last();
                sig += ToGlslParamType(l) + " " + l.Name;
            }

            return sig;
        }

        internal static string GetSignature(MethodDefinition m)
        {
            return ToGlslType(m.ReturnType) + " " +
                Shader.GetMethodName(m) + "(" +
                GetParameterString(m) + ")";
        }

        private void RegisterMethod(MethodDefinition m)
        {
            // generate signature
            var neededType = _attr.AttributeType.Resolve();
            var attr = m.CustomAttributes.FirstOrDefault(a => a.AttributeType.Resolve().MetadataToken == neededType.MetadataToken);

            if (attr == null)
                throw new Exception("Called shader method has no " + neededType.Name + Environment.NewLine + GetSignature(m));

            if ((bool)attr.ConstructorArguments.FirstOrDefault().Value)
                throw new Exception("Cannot call shader entry point.");

            _functions.Add(new Tuple<string, string>(GetSignature(m), m.DeclaringType.FullName + "." + m.Name));
        }

        private StringBuilder ArgsToString(ICollection<Expression> args)
        {
            var result = new StringBuilder();
            if (args.Count <= 0)
                return result;

            foreach (var v in args.Take(args.Count - 1))
            {
                result.Append(v.AcceptVisitor(this, 0));
                result.Append(", ");
            }

            result.Append(args.Last().AcceptVisitor(this, 0));
            return result;
        }

        private static StringBuilder Indent(AstNode node, StringBuilder b)
        {
            if (node is BlockStatement)
                return b;

            var res = new StringBuilder();

            res.Append("\t");
            res.Append(b.Replace(Environment.NewLine, Environment.NewLine + "\t"));

            return res;
        }

        public GlslVisitor(BlockStatement block, CustomAttribute attr)
        {
            _attr = attr;

            var trans1 = new ReplaceMethodCallsWithOperators();
            var trans2 = new RenameLocals();
            ((IAstTransform)trans1).Run(block);
            trans2.Run(block);

            Result = block.AcceptVisitor(this, 0).ToString();
            Result += Environment.NewLine;
        }

        public StringBuilder VisitBlockStatement(BlockStatement blockStatement, int data)
        {
            var result = new StringBuilder();

            result.Append(Environment.NewLine).Append("{");

            foreach (var stm in blockStatement.Statements)
                result.Append(Environment.NewLine).Append(Indent(stm, stm.AcceptVisitor(this, data)));

            result.Append(Environment.NewLine).Append("}");

            return result;
        }

        public StringBuilder VisitExpressionStatement(ExpressionStatement expressionStatement, int data)
        {
            return expressionStatement.Expression.AcceptVisitor(this, data).Append(";");
        }

        public StringBuilder VisitAssignmentExpression(AssignmentExpression assignmentExpression, int data)
        {
            var result = assignmentExpression.Left.AcceptVisitor(this, data);

            switch (assignmentExpression.Operator)
            {
                case AssignmentOperatorType.Assign:
                    result.Append(" = ");
                    break;
                case AssignmentOperatorType.Add:
                    result.Append(" += ");
                    break;
                case AssignmentOperatorType.BitwiseAnd:
                    result.Append(" &= ");
                    break;
                case AssignmentOperatorType.BitwiseOr:
                    result.Append(" |= ");
                    break;
                case AssignmentOperatorType.Divide:
                    result.Append(" /= ");
                    break;
                case AssignmentOperatorType.ExclusiveOr:
                    result.Append(" ^= ");
                    break;
                case AssignmentOperatorType.Modulus:
                    result.Append(" %= ");
                    break;
                case AssignmentOperatorType.Multiply:
                    result.Append(" *= ");
                    break;
                case AssignmentOperatorType.ShiftLeft:
                    result.Append(" <<= ");
                    break;
                case AssignmentOperatorType.ShiftRight:
                    result.Append(" >>= ");
                    break;
                case AssignmentOperatorType.Subtract:
                    result.Append(" -= ");
                    break;
                default:
                    throw new NotImplementedException();
            }

            result.Append(assignmentExpression.Right.AcceptVisitor(this, data));

            return result;
        }

        public StringBuilder VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, int data)
        {
            var result = new StringBuilder();

            if (!(memberReferenceExpression.Target is ThisReferenceExpression))
                result.Append(memberReferenceExpression.Target.AcceptVisitor(this, data)).Append(".");

            var def = memberReferenceExpression.Annotation<IMemberDefinition>();
            if (def != null)
                return result.Append(Shader.ResolveName(def));

            var fref = memberReferenceExpression.Annotation<FieldReference>();
            if (fref != null)
                return result.Append(Shader.ResolveName(fref.Resolve()));

            throw new NotImplementedException();
        }

        public StringBuilder VisitInvocationExpression(InvocationExpression invocationExpression, int data)
        {
            var result = new StringBuilder();

            var mref = invocationExpression.Annotation<MethodReference>();
            var m = mref != null ? mref.Resolve() : invocationExpression.Annotation<MethodDefinition>();

            if (m.DeclaringType.MetadataToken.ToInt32() == typeof(ShaderDefinition).MetadataToken)
                return result.Append(m.Name).Append("(").Append(ArgsToString(invocationExpression.Arguments)).Append(")");

            RegisterMethod(m);
            result.Append(Shader.GetMethodName(m));
            result.Append("(").Append(ArgsToString(invocationExpression.Arguments)).Append(")");

            return result;
        }

        public StringBuilder VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, int data)
        {
            return objectCreateExpression.Type.AcceptVisitor(this, data).Append("(").Append(
                ArgsToString(objectCreateExpression.Arguments)).Append(")");
        }

        public StringBuilder VisitMemberType(MemberType memberType, int data)
        {
            ValidateType(memberType.Annotation<TypeReference>());
            return new StringBuilder(memberType.MemberName);
        }

        public StringBuilder VisitSimpleType(SimpleType simpleType, int data)
        {
            // this cast might not work for all cases...
            ValidateType((TypeReference)simpleType.Annotation<MemberReference>());
            return new StringBuilder(simpleType.ToString());
        }

        public StringBuilder VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, int data)
        {
            var result = new StringBuilder();

            if (primitiveExpression.Value.GetType() == typeof(float))
            {
                var s = ((float)primitiveExpression.Value).ToString(CultureInfo.InvariantCulture.NumberFormat);
                result.Append(s);
                if (!s.Contains("."))
                   result.Append(".0");

                result.Append("f");
                return result;
            }

            if (primitiveExpression.Value.GetType() == typeof(double))
            {
                var s = ((double)primitiveExpression.Value).ToString(CultureInfo.InvariantCulture.NumberFormat);
                result.Append(s);
                if (!s.Contains("."))
                    result.Append(".0");

                result.Append("lf");
                return result;
            }

            if (primitiveExpression.Value.GetType() == typeof(uint))
            {
                var s = ((uint)primitiveExpression.Value).ToString(CultureInfo.InvariantCulture.NumberFormat);
                result.Append(s).Append("u");
                return result;
            }

            return result.Append(primitiveExpression.Value.ToString());
        }

        public StringBuilder VisitCastExpression(CastExpression castExpression, int data)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        public StringBuilder VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, int data)
        {
            var result = new StringBuilder("(");

            result.Append(binaryOperatorExpression.Left.AcceptVisitor(this, data));

            switch (binaryOperatorExpression.Operator)
            {
                case BinaryOperatorType.Multiply:
                    result.Append(" * ");
                    break;
                case BinaryOperatorType.Divide:
                    result.Append(" / ");
                    break;
                case BinaryOperatorType.Add:
                    result.Append(" + ");
                    break;
                case BinaryOperatorType.Subtract:
                    result.Append(" - ");
                    break;
                case BinaryOperatorType.Modulus:
                    result.Append(" % ");
                    break;
                case BinaryOperatorType.BitwiseOr:
                    result.Append(" | ");
                    break;
                case BinaryOperatorType.BitwiseAnd:
                    result.Append(" & ");
                    break;
                case BinaryOperatorType.ExclusiveOr:
                    result.Append(" ^ ");
                    break;
                case BinaryOperatorType.ShiftLeft:
                    result.Append(" << ");
                    break;
                case BinaryOperatorType.ShiftRight:
                    result.Append(" >> ");
                    break;
                case BinaryOperatorType.LessThan:
                    result.Append(" < ");
                    break;
                case BinaryOperatorType.GreaterThan:
                    result.Append(" > ");
                    break;
                case BinaryOperatorType.LessThanOrEqual:
                    result.Append(" <= ");
                    break;
                case BinaryOperatorType.GreaterThanOrEqual:
                    result.Append(" >= ");
                    break;
                case BinaryOperatorType.Equality:
                    result.Append(" == ");
                    break;
                case BinaryOperatorType.InEquality:
                    result.Append(" != ");
                    break;
                default:
                    throw new NotImplementedException();
            }

            result.Append(binaryOperatorExpression.Right.AcceptVisitor(this, data)).Append(")");

            return result;
        }

        public StringBuilder VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, int data)
        {
            var result = new StringBuilder();

            var exp = unaryOperatorExpression.Expression.AcceptVisitor(this, data);

            switch (unaryOperatorExpression.Operator)
            {
                case UnaryOperatorType.Decrement:
                    return result.Append("--").Append(exp);
                case UnaryOperatorType.Increment:
                    return result.Append("++").Append(exp);
                case UnaryOperatorType.Minus:
                    return result.Append("-").Append(exp);
                case UnaryOperatorType.Plus:
                    return result.Append("+").Append(exp);
                case UnaryOperatorType.BitNot:
                    return result.Append("~").Append(exp);
                case UnaryOperatorType.Not:
                    return result.Append("!").Append(exp);
                case UnaryOperatorType.PostDecrement:
                    return result.Append(exp).Append("--");
                case UnaryOperatorType.PostIncrement:
                    return result.Append(exp).Append("++");
            }

            throw new NotImplementedException();
        }

        public StringBuilder VisitReturnStatement(ReturnStatement returnStatement, int data)
        {
            var result = new StringBuilder();

            result.Append("return");

            var exp = returnStatement.Expression.AcceptVisitor(this, data);
            if (exp.Length > 0)
                result.Append(" ").Append(exp);

            result.Append(";");

            return result;
        }

        public StringBuilder VisitIdentifierExpression(IdentifierExpression identifierExpression, int data)
        {
            return new StringBuilder(identifierExpression.Identifier);
        }

        public StringBuilder VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, int data)
        {
            var result = new StringBuilder();

            var type = variableDeclarationStatement.Type.AcceptVisitor(this, data);
            foreach (var v in variableDeclarationStatement.Variables)
                result.Append(type).Append(" ").Append(v.AcceptVisitor(this, data)).Append(";");

            return result;
        }

        public StringBuilder VisitVariableInitializer(VariableInitializer variableInitializer, int data)
        {
            var result = new StringBuilder(variableInitializer.Name);

            if (!variableInitializer.Initializer.IsNull)
                result.Append(" = ").Append(variableInitializer.Initializer.AcceptVisitor(this, data));

            return result;
        }

        public StringBuilder VisitPrimitiveType(PrimitiveType primitiveType, int data)
        {
            return new StringBuilder(primitiveType.Keyword);
        }

        public StringBuilder VisitWhileStatement(WhileStatement whileStatement, int data)
        {
            var result = new StringBuilder("while (").Append(whileStatement.Condition.AcceptVisitor(this, data)).Append(")");

            var stmt = whileStatement.EmbeddedStatement;
            result.Append(Indent(stmt, stmt.AcceptVisitor(this, data)));

            return result;
        }

        public StringBuilder VisitDoWhileStatement(DoWhileStatement doWhileStatement, int data)
        {
            var result = new StringBuilder("do");

            var stmt = doWhileStatement.EmbeddedStatement;
            result.Append(Indent(stmt, stmt.AcceptVisitor(this, data)));

            result.Append("while (").Append(doWhileStatement.Condition.AcceptVisitor(this, data)).Append(");");

            return result;
        }

        public StringBuilder VisitIfElseStatement(IfElseStatement ifElseStatement, int data)
        {
            var result = new StringBuilder("if (").Append(ifElseStatement.Condition.AcceptVisitor(this, data)).Append(")");

            var trueSection = ifElseStatement.TrueStatement;
            result.Append(Indent(trueSection, trueSection.AcceptVisitor(this, data)));

            var elseSection = ifElseStatement.FalseStatement;
            if (elseSection != null)
            {
                result.Append(Environment.NewLine + "else");
                result.Append(Indent(elseSection, elseSection.AcceptVisitor(this, data)));
            }

            return result;
        }

        public StringBuilder VisitSwitchStatement(SwitchStatement switchStatement, int data)
        {
            var result = new StringBuilder("switch (").Append(switchStatement.Expression.AcceptVisitor(this, data)).Append(")");

            result.Append(Environment.NewLine + "{");

            foreach (var section in switchStatement.SwitchSections)
                result.Append(Environment.NewLine).Append(Indent(section, section.AcceptVisitor(this, data)));

            result.Append(Environment.NewLine + "}");

            return result;
        }

        public StringBuilder VisitSwitchSection(SwitchSection switchSection, int data)
        {
            var result = new StringBuilder();

            foreach (var label in switchSection.CaseLabels)
            {
                result.Append(Indent(label, label.AcceptVisitor(this, data)));
                result.Append(Environment.NewLine);
            }

            result.Append(Environment.NewLine).Append("{");

            foreach (var stmt in switchSection.Statements)
                result.Append(Environment.NewLine).Append(Indent(stmt, stmt.AcceptVisitor(this, data)));

            result.Append(Environment.NewLine).Append("}");

            return result;
        }

        public StringBuilder VisitCaseLabel(CaseLabel caseLabel, int data)
        {
            return new StringBuilder("case ").Append(caseLabel.Expression.AcceptVisitor(this, data)).Append(":");
        }

        public StringBuilder VisitBreakStatement(BreakStatement breakStatement, int data)
        {
            return new StringBuilder("break;");
        }

        public StringBuilder VisitContinueStatement(ContinueStatement continueStatement, int data)
        {
            return new StringBuilder("continue;");
        }

        public StringBuilder VisitForStatement(ForStatement forStatement, int data)
        {
            var result = new StringBuilder("for (");

            var initializers = forStatement.Initializers.ToList();
            var initializerCount = initializers.Count;
            for (var i = 0; i < initializerCount; i++)
            {
                var initializer = initializers[i];
                result.Append(initializer.AcceptVisitor(this, data));

                if (i != initializerCount - 1)
                    result.Append(", ");
            }

            result.Append(";");

            var condition = forStatement.Condition;
            if (condition != null)
            {
                result.Append(" ");
                result.Append(condition.AcceptVisitor(this, data));
            }

            result.Append(";");

            var iterators = forStatement.Iterators.ToList();
            var iteratorCount = iterators.Count;

            if (iteratorCount != 0)
                result.Append(" ");

            for (var i = 0; i < iteratorCount; i++)
            {
                var iterator = iterators[i];
                result.Append(iterator.AcceptVisitor(this, data));

                if (i != iteratorCount - 1)
                    result.Append(", ");
            }

            result.Append(")");

            var stmt = forStatement.EmbeddedStatement;
            result.Append(Indent(stmt, stmt.AcceptVisitor(this, data)));

            return result;
        }

        public StringBuilder VisitDirectionExpression(DirectionExpression directionExpression, int data)
        {
            // at invocation time we wont pass out or inout keywords so
            // this just passes through
            return directionExpression.Expression.AcceptVisitor(this, data);
        }
    }
}
