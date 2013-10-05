using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeLight.Common.Desktop;

namespace CodeLight.Common.Desktop
{
    public abstract class ViewModelBase : NotifyObject
    {
        private object _model;

        public virtual object Model
        {
            get { return _model; }
            set
            {
                _model = value;
                ModelChanged();
            }
        }

        protected abstract void ModelChanged();
    }

    public class ViewModelBase<T> : ViewModelBase
    {
        public new virtual T Model
        {
            get { return (T)base.Model; }
            set { base.Model = value; }
        }

        protected override void ModelChanged()
        {
            OnPropertyChanged("Model");
        }
    }
}
