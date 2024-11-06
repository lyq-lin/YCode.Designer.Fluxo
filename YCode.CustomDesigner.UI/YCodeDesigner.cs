﻿using System.Runtime.CompilerServices;

namespace YCode.CustomDesigner.UI;

public partial class YCodeDesigner : MultiSelector
{
    static YCodeDesigner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(YCodeDesigner),
            new FrameworkPropertyMetadata(typeof(YCodeDesigner))
        );

        FocusableProperty.OverrideMetadata(typeof(YCodeDesigner), new FrameworkPropertyMetadata(false));
    }

    private bool? _isLoaded = false;

    private readonly TranslateTransform _translateTransform = new();
    private readonly ScaleTransform _scaleTransform = new();

    public YCodeDesigner()
    {
        this.AddHandler(YCodeNode.DragStartedEvent, new DragStartedEventHandler(this.OnNodeDragStarted));
        this.AddHandler(YCodeNode.DragDeltaEvent, new DragDeltaEventHandler(this.OnNodeDragDelta));
        this.AddHandler(YCodeNode.DragCompletedEvent, new DragCompletedEventHandler(this.OnNodeDragCompleted));

        var transform = new TransformGroup();
        transform.Children.Add(_scaleTransform);
        transform.Children.Add(_translateTransform);

        this.SetValue(ViewportTransformPropertyKey, transform);

        this.Loaded += OnLoaded;
    }

    protected internal Panel ItemsHost { get; private set; } = default!;

    internal bool IsPanning { get; set; }

    internal Point? Point { get; set; }

    #region Dependency Properties

    protected internal static readonly DependencyPropertyKey ViewportTransformPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ViewportTransform),
            typeof(Transform),
            typeof(YCodeDesigner),
            new FrameworkPropertyMetadata(new TransformGroup())
        );

    public static readonly DependencyProperty ViewportTransformProperty =
        ViewportTransformPropertyKey.DependencyProperty;

    public static readonly DependencyProperty ItemsExtentProperty = DependencyProperty.Register(
        nameof(ItemsExtent), typeof(Rect), typeof(YCodeDesigner), new PropertyMetadata(default(Rect)));

    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
        nameof(Zoom), typeof(double), typeof(YCodeDesigner), new PropertyMetadata(1d));

    public static readonly DependencyProperty MaxZoomProperty = DependencyProperty.Register(
        nameof(MaxZoom), typeof(double), typeof(YCodeDesigner), new PropertyMetadata(2d));

    public static readonly DependencyProperty MinZoomProperty = DependencyProperty.Register(
        nameof(MinZoom), typeof(double), typeof(YCodeDesigner), new PropertyMetadata(0.5d));

    public static readonly DependencyProperty ViewportLocationProperty = DependencyProperty.Register(
        nameof(ViewportLocation), typeof(Point), typeof(YCodeDesigner),
        new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnViewportLocationChanged));

    private static void OnViewportLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is YCodeDesigner designer && e.NewValue is Point translate)
        {
            designer._translateTransform.X = -translate.X * designer.Zoom;

            designer._translateTransform.Y = -translate.Y * designer.Zoom;

            designer.RaiseEvent(nameof(designer.ViewportUpdated), new RoutedEventArgs());
        }
    }

    public static readonly DependencyProperty ViewportSizeProperty = DependencyProperty.Register(
        nameof(ViewportSize), typeof(Size), typeof(YCodeDesigner), new PropertyMetadata(default(Size)));

    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
        nameof(SelectedItems), typeof(IList), typeof(YCodeDesigner), new PropertyMetadata(default(IList)));

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(YCodeSource), typeof(YCodeDesigner));

    public static readonly DependencyProperty LinesProperty = DependencyProperty.Register(
        nameof(Lines), typeof(IEnumerable), typeof(YCodeDesigner));

    public static readonly DependencyProperty ItemAdapterProperty = DependencyProperty.Register(
        nameof(ItemAdapter), typeof(IYCodeAdapter), typeof(YCodeDesigner));

    public static readonly DependencyProperty LineTypeProperty = DependencyProperty.Register(
        nameof(LineType), typeof(LineType), typeof(YCodeDesigner), new PropertyMetadata(LineType.Bezier));

    public LineType LineType
    {
        get => (LineType)GetValue(LineTypeProperty);
        set => SetValue(LineTypeProperty, value);
    }

    public IYCodeAdapter? ItemAdapter
    {
        get => (IYCodeAdapter?)GetValue(ItemAdapterProperty);
        set => SetValue(ItemAdapterProperty, value);
    }

    public IEnumerable Lines
    {
        get => (IEnumerable)GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    public YCodeSource? Source
    {
        get => (YCodeSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public new IList? SelectedItems
    {
        get => (IList?)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    public Size ViewportSize
    {
        get => (Size)GetValue(ViewportSizeProperty);
        set => SetValue(ViewportSizeProperty, value);
    }

    public Point ViewportLocation
    {
        get => (Point)GetValue(ViewportLocationProperty);
        set => SetValue(ViewportLocationProperty, value);
    }

    public double MinZoom
    {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public Rect ItemsExtent
    {
        get => (Rect)GetValue(ItemsExtentProperty);
        set => SetValue(ItemsExtentProperty, value);
    }

    public Transform ViewportTransform => (Transform)GetValue(ViewportTransformProperty);

    #endregion

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        this.ItemsHost = this.GetTemplateChild("PART_ItemsHost") as Panel ??
                         throw new InvalidOperationException(
                             $"PART_ItemsHost is missing or is not of type {nameof(Panel)}.");

        this.ApplyRenderingOptimizations();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == SourceProperty)
        {
            if (e.NewValue is YCodeSource source)
            {
                this.ItemsSource = source.Nodes;
                this.Lines = source.Lines;

                return;
            }
        }

        base.OnPropertyChanged(e);
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new YCodeNode(this)
        {
            RenderTransform = new TranslateTransform()
        };
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is YCodeNode;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (Mouse.Captured == null || this.IsMouseCaptured)
        {
            this.Focus();

            this.CaptureMouseSafe();

            this.IsPanning = true;

            this.Cursor = Cursors.SizeAll;

            this.Point = Mouse.GetPosition(this);

            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var mouse = e.GetPosition(this);

        if (this.IsPanning && this.IsMouseCaptureWithin)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                this.IsPanning = false;

                this.Cursor = Cursors.Arrow;

                this.Point = null;

                if (this.IsMouseCaptured)
                {
                    this.ReleaseMouseCapture();
                }

                return;
            }

            if (this.Point.HasValue)
            {
                var value = mouse - this.Point.Value;

                this.ViewportLocation -= value / this.Zoom;

                this.Point = mouse;
            }
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (this.IsMouseCaptured)
        {
            this.ReleaseMouseCapture();
        }

        if (this.IsPanning)
        {
            this.IsPanning = false;

            this.Cursor = Cursors.Arrow;

            this.Point = null;
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded.HasValue || !_isLoaded.Value)
        {
            _isLoaded = false;

            var mounting = new MountingEventArgs();

            this.RaiseEvent<MountingEventArgs>(nameof(Mounting), mounting);

            if (!mounting.Cancel)
            {
                if (this.ItemAdapter != null)
                {
                    var source = await this.ItemAdapter.ImportAsync(mounting.Value);

                    this.Source ??= new YCodeSource();

                    this.Source.Nodes.Clear();

                    this.Source.Lines.Clear();

                    await this.CopyToAsync(source.Nodes, this.Source.Nodes);

                    await this.CopyToAsync(source.Lines, this.Source.Lines);
                }

                this.RaiseEvent<MountedEventArgs>(nameof(Mounted), new MountedEventArgs(this.Source));
            }

            _isLoaded = true;
        }
    }

    private void ApplyRenderingOptimizations()
    {
        ItemsHost.CacheMode = new BitmapCache(1.0 / 1.0);
    }

    private async Task CopyToAsync(IList source, IList destination)
    {
        await Task.Run(() =>
        {
            foreach (var item in source)
            {
                destination.Add(item);
            }
        });
    }

    public DependencyObject GetContainerForLineOverride(YCodeLineContainer container)
    {
        return new YCodeLine(this, container);
    }

    public bool IsLineItsOwnContainerOverride(YCodeLineContainer container, object item)
    {
        return item is YCodeLine;
    }

    private void OnNodeDragStarted(object sender, DragStartedEventArgs e)
    {
        if (e.OriginalSource is YCodeNode node)
        {
            this.RaiseEvent<NodeDragStartedEventArgs>(nameof(DragStared), new NodeDragStartedEventArgs(node));
        }
    }

    private void OnNodeDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (e.OriginalSource is YCodeNode node)
        {
            this.RaiseEvent<NodeDragCompletedEventArgs>(nameof(DragCompleted), new NodeDragCompletedEventArgs(node));
        }
    }

    private void OnNodeDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (e.OriginalSource is YCodeNode node)
        {
            var left = e.HorizontalChange;

            var top = e.VerticalChange;

            //TODO: MoveRange

            node.Location = new Point(left, top);

            this.RaiseEvent<NodeDragDeltaEventArgs>(nameof(DragDelta), new NodeDragDeltaEventArgs(node));
        }
    }
}