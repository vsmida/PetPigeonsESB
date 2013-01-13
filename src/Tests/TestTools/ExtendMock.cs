using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Moq;

namespace Tests.TestTools
{
    public static class ExtendMock
    {
        public static void CaptureVariable<T, TCapture>(this Mock<T> mock,Expression<Func<TCapture>> closureFunc, Expression<Action<T, TCapture>> argumentToCapture) where T : class
        {

            var newExpression = CreateSetupExpression(argumentToCapture);
            if (argumentToCapture.Body.NodeType != ExpressionType.Call)
                throw new ArgumentException("Can only work on method calls");

            var body = (MethodCallExpression)argumentToCapture.Body;
            var parameters = new List<ParameterExpression>();
            var paramToCapture = GetParameterToCapture(body, parameters);
            var lambdaToCallCompiled = CreateCallbackLambda(closureFunc, paramToCapture, parameters);
            var moqCallbackMethod = GetRightCallbackOverload(mock, newExpression, parameters);         
            moqCallbackMethod.Invoke(mock.Setup(newExpression), new object[] { lambdaToCallCompiled});
        }

        private static Expression<Action<T>> CreateSetupExpression<T, TCapture>(Expression<Action<T, TCapture>> argumentToCapture) where T : class
        {
            var visitor = new ParameterReplaceVisitor();
            var parameterExpression = argumentToCapture.Parameters[1];
            Expression<Action> expr = () => It.IsAny<TCapture>();
            visitor.ReplaceParameter(parameterExpression, expr.Body);
            var newExpressionBody = visitor.Visit(argumentToCapture.Body);
            var newExpressionParameter = argumentToCapture.Parameters[0];
            var newExpression = Expression.Lambda<Action<T>>(newExpressionBody, new[] {newExpressionParameter});
            return newExpression;
        }

        private static MethodInfo GetRightCallbackOverload<T>(Mock<T> mock, Expression<Action<T>> newExpression, List<ParameterExpression> parameters)
            where T : class
        {
            var methodCall = mock.Setup(newExpression).GetType().GetMethods().Single(
                x => x.Name == "Callback" && x.GetGenericArguments().Count() == parameters.Count);
            var genericMethod = methodCall.MakeGenericMethod(parameters.Select(x => x.Type).ToArray());
            return genericMethod;
        }

        private static ParameterExpression GetParameterToCapture(MethodCallExpression body, List<ParameterExpression> parameters)
        {
            ParameterExpression paramToCapture = null;
            foreach (Expression argument in body.Arguments)
            {
                var expression = Expression.Parameter(argument.Type);
                parameters.Add(expression);
                if (argument.NodeType == ExpressionType.Parameter)
                    paramToCapture = expression;
            }
            return paramToCapture;
        }

        private static Delegate CreateCallbackLambda<TCapture>(Expression<Func<TCapture>> closureFunc, ParameterExpression paramToCapture,
                                                              List<ParameterExpression> parameters)
        {
            LabelTarget voidLabel = Expression.Label();
            LabelExpression returnVoid = Expression.Label(voidLabel);
            var returnExprVoid = Expression.Return(voidLabel);
            var assignExpr = Expression.Assign(closureFunc.Body, paramToCapture);
            var blockExprVoid = Expression.Block(assignExpr, returnExprVoid, returnVoid);
            var lamdaToCall = Expression.Lambda(blockExprVoid, parameters);

            var lambdaToCallCompiled = lamdaToCall.Compile();
            return lambdaToCallCompiled;
        }


        private class ParameterReplaceVisitor: ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, Expression> _nodesToReplace = new Dictionary<ParameterExpression, Expression>();

            public void ReplaceParameter(ParameterExpression parameter, Expression constant)
            {
                _nodesToReplace[parameter] = constant;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                Expression constant;
                if (_nodesToReplace.TryGetValue(node, out constant))
                    return constant;

                return base.VisitParameter(node);
            }
        }

 

    }
}