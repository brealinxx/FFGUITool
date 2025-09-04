using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FFGUITool.ViewModels
{
    /// <summary>
    /// ViewModel基类，提供属性通知功能
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        private bool _isInitialized;

        /// <summary>
        /// 指示ViewModel是否已初始化
        /// </summary>
        public bool IsInitialized
        {
            get => _isInitialized;
            protected set => SetProperty(ref _isInitialized, value);
        }

        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public virtual void Initialize()
        {
            if (!IsInitialized)
            {
                OnInitialize();
                IsInitialized = true;
            }
        }

        /// <summary>
        /// 异步初始化ViewModel
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            if (!IsInitialized)
            {
                await OnInitializeAsync();
                IsInitialized = true;
            }
        }

        /// <summary>
        /// 同步初始化逻辑，由子类重写
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 异步初始化逻辑，由子类重写
        /// </summary>
        protected virtual Task OnInitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// 清理资源
        /// </summary>
        public virtual void Dispose() { }
    }
}