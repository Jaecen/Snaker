using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CrappyGame;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Snaker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : GameWindow
	{
		Storyboard WalkStoryboard { get; set; }
		Storyboard JumpStoryboard { get; set; }
		Storyboard EndGameStoryboard { get; set; }
		Storyboard LevelCompleteSotryboard { get; set; }

		List<Rect> Snakes { get; set; }
		List<Image> SnakeImages { get; set; }
		List<DispatcherTimer> SnakeTimers { get; set; }

		bool GameOver = false;
		bool Jumping = false;
		int RemainingSnakes = 0;
		bool Advancing = false;
		double CupToX;

		private int _Level;
		protected int Level
		{
			get
			{
				return _Level;
			}
			set
			{
				_Level = value;
				LevelDisplay = String.Format("Level {0}", value);
			}
		}

		public static readonly DependencyProperty LevelDisplayProperty = DependencyProperty.Register("LevelDisplay", typeof(String), typeof(MainWindow));
		public string LevelDisplay
		{
			get
			{
				return (string)GetValue(LevelDisplayProperty);
			}
			set
			{
				SetValue(LevelDisplayProperty, value);
			}
		}

		public MainWindow()
		{
			InitializeComponent();

			WalkStoryboard = (Storyboard)FindResource("WalkStoryboard");
			
			JumpStoryboard = (Storyboard)FindResource("JumpStoryboard");
			JumpStoryboard.Completed += (o, e) => 
			{
				Jumping = false;
				
				if(GameOver)
					return;

				WalkStoryboard.Begin(this, true); 
			};
			
			EndGameStoryboard = (Storyboard)FindResource("EndGameStoryboard");
			
			LevelCompleteSotryboard = (Storyboard)FindResource("LevelCompleteSotryboard");
			LevelCompleteSotryboard.Completed += (o, e) =>
			{
				cupTranslate.X = CupToX;
				DispatcherTimer advanceTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Normal, AdvanceTimer_Tick, Dispatcher);
				LevelCompleteSotryboard.Stop();
			};

			Input.RegisterKeyDown(Key.Space, GirlJump);
			Input.RegisterKeyDown(Key.Space, (k, gt) => { if(GameOver) ResetGame(); });

			Snakes = new List<Rect>();
			SnakeImages = new List<Image>();
			SnakeTimers = new List<DispatcherTimer>();

			ResetGame();
		}

		private void ResetGame()
		{
			GameOver = false;

			Level = 0;

			girlRotate.Angle = 0;
			girlTranslate.X = 0;
			girlTranslate.Y = 0;

			cupRotate.Angle = 0;
			cupTranslate.X = 0;
			cupTranslate.Y = 0;

			Jumping = true;
			JumpStoryboard.Begin(this, true);

			AdvanceLevel();
		}

		private void AdvanceLevel()
		{
			Level++;
			RemainingSnakes = 0;

			Snakes.Clear();

			foreach(var snakeImage in SnakeImages)
				Container.Children.Remove(snakeImage);

			SnakeImages.Clear();

			foreach(var snakeTimer in SnakeTimers)
				snakeTimer.Stop();
			
			SnakeTimers.Clear();

			// Always have the snakes in the same pattern
			Random rng = new Random(Level);
			double cumulativeTime = 0;

			for(int i = 0; i < Level + 2; i++)
			{
				// Snakes will appear every 3 to 8 seconds
				double snakeDuration = 3 + (rng.NextDouble() * 5);
				cumulativeTime += snakeDuration;
				DispatcherTimer snakeTimer = new DispatcherTimer(TimeSpan.FromSeconds(cumulativeTime), DispatcherPriority.Normal, SnakeTimer_Tick, Dispatcher);
				SnakeTimers.Add(snakeTimer);
				RemainingSnakes++;
			}

			Advancing = false;
		}

		protected void SnakeTimer_Tick(object sender, EventArgs e)
		{

			DispatcherTimer timer = ((DispatcherTimer)sender);
			timer.Stop();
			SnakeTimers.Remove(timer);

			if(GameOver)
				return;

			Rect snake = new Rect(Container.ActualWidth, Container.ActualHeight - 150, 100, 100);
			Snakes.Add(snake);

			BitmapImage bitmap = (BitmapImage)FindResource("Snake");
			Image image = new Image();
			image.Width = snake.Width;
			snake.Height = image.ActualHeight;
			image.Source = bitmap;
			SnakeImages.Add(image);
			Container.Children.Add(image);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			GroundLine.X2 = this.ActualWidth;
			DoubleAnimation horizAnim = (DoubleAnimation)LevelCompleteSotryboard.Children.First();
			Rect girlBox = GetBoundingBox(GirlImage);
			Rect cupBox = GetBoundingBox(GoldenPImage);
			CupToX = (girlBox.X + girlBox.Width) - (cupBox.X - cupBox.Width);
			horizAnim.To = CupToX;
		}

		protected override void RenderFrame(GameTime time)
		{
			base.RenderFrame(time);

			if(GameOver || Advancing)
				return;

			DrawSnakes(time);
			CheckCollisions(time);

			if(RemainingSnakes <= 0)
				GetGoldenCup();
		}

		private void GetGoldenCup()
		{
			Advancing = true;
			LevelCompleteSotryboard.Begin(this, true);
		}

		private void AdvanceTimer_Tick(object sender, EventArgs e)
		{
			((DispatcherTimer)sender).Stop();

			LevelCompleteSotryboard.Stop();

			cupRotate.Angle = 0;
			cupTranslate.Y = 0;
			cupTranslate.X = 0;

			LevelCompleteSotryboard.Resume();

			AdvanceLevel();
		}

		private void CheckCollisions(GameTime time)
		{
			Rect girlBox = GetBoundingBox(GirlImage);

			foreach(var snake in Snakes)
				if(girlBox.IntersectsWith(snake))
					EndGame();
		}

		private Rect GetBoundingBox(Image control)
		{
			var xform = control.TransformToAncestor(Container);
			Point corner = xform.Transform(new Point(0, 0));
			Rect box = new Rect(corner.X, corner.Y, control.ActualWidth, control.ActualHeight);
			return box;
		}

		private void EndGame()
		{
			GameOver = true;

			JumpStoryboard.Stop();
			WalkStoryboard.Stop();

			girlRotate.Angle = 0;

			EndGameStoryboard.Begin(this, true);
		}

		private void DrawSnakes(GameTime time)
		{
			for(int i = 0; i < Snakes.Count; i++)
			{
				Rect snake = Snakes[i];
				Image snakeImage = SnakeImages[i];

				snake.Offset(time.ElapsedSeconds * -(Container.ActualWidth / 5), 0);

				Snakes[i] = snake;

				snakeImage.SetValue(Canvas.LeftProperty, snake.Left);
				snakeImage.SetValue(Canvas.TopProperty, snake.Top);

				if(snake.X + snake.Width < 0)
				{
					SnakeImages.Remove(snakeImage);
					Container.Children.Remove(snakeImage);
					Snakes.Remove(snake);
					RemainingSnakes--;
				}
			}
		}

		protected void GirlJump(Key key, GameTime time)
		{
			if(GameOver)
				return;

			if(Jumping)
				return;

			Jumping = true;

			WalkStoryboard.Stop(this);
			JumpStoryboard.Begin(this, true);
		}
	}
}
