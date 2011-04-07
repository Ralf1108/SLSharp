﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using IIS.SLSharp.Core;

namespace IIS.SLSharp.Translation
{
    internal sealed partial class GlslVisitor
    {
        public StringBuilder VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, int data)
        {
            return null;
        }

        public StringBuilder VisitCastExpression(CastExpression castExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitConditionalExpression(ConditionalExpression conditionalExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDirectionExpression(DirectionExpression directionExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitIndexerExpression(IndexerExpression indexerExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, int data)
        {
            return null;
        }

        public StringBuilder VisitUncheckedExpression(UncheckedExpression uncheckedExpression, int data)
        {
            return new StringBuilder();
        }

        public StringBuilder VisitEmptyExpression(EmptyExpression emptyExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitBreakStatement(BreakStatement breakStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitContinueStatement(ContinueStatement continueStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDoWhileStatement(DoWhileStatement doWhileStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitEmptyStatement(EmptyStatement emptyStatement, int data)
        {
            return new StringBuilder();
        }

        public StringBuilder VisitForeachStatement(ForeachStatement foreachStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitForStatement(ForStatement forStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitIfElseStatement(IfElseStatement ifElseStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitSwitchStatement(SwitchStatement switchStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitSwitchSection(SwitchSection switchSection, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCaseLabel(CaseLabel caseLabel, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUncheckedStatement(UncheckedStatement uncheckedStatement, int data)
        {
            return new StringBuilder();
        }

        public StringBuilder VisitSimpleType(SimpleType simpleType, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitComposedType(ComposedType composedType, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitArraySpecifier(ArraySpecifier arraySpecifier, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitIdentifier(Identifier identifier, int data)
        {
            throw new NotImplementedException();
        }
    }
}