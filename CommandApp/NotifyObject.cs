using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace CodeLight.Common.Desktop
{
    public  class NotifyObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private static string GetPropertyNameFromExpression<T>(Expression<Func<T>> property)
        {
            var lambda = (LambdaExpression)property;
            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)lambda.Body;
            }

            return memberExpression.Member.Name;
        }

        protected void OnPropertyChanged<T>(Expression<Func<T>> property)
        {
            OnPropertyChanged(GetPropertyNameFromExpression(property));
        }
    }
}