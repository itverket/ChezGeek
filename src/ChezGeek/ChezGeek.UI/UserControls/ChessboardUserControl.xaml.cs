using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Akka.Actor;
using ChezGeek.Common.Actors;
using ChezGeek.Common.Messages;
using ChezGeek.UI.ViewModels;
using Geek2k16.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Attributes;
using LiveCharts;
using LiveCharts.Wpf;
using Brushes = System.Windows.Media.Brushes;

namespace ChezGeek.UI.UserControls
{
    /// <summary>
    /// Interaction logic for ChessboardUserControl.xaml
    /// </summary>
    public partial class ChessboardUserControl
    {
        private readonly Chessboard _viewModel = new Chessboard();
        private IActorRef _board;
        private readonly ActorSystem _actorSystem;
        private ChessBoardStateViewModel _chessBoardState;
        private bool _stopRun;
        private bool _isBusy;

        public SeriesCollection PerceivedStrengthsCollection { get; set; }
        public List<string> Labels { get; set; }

        public Func<double, string> Formatter { get; set; }


        public ChessboardUserControl()
        {
            InitializeComponent();
            _actorSystem = ActorSystem.Create("ChezCluster");
            _board = _actorSystem.ActorOf(Props.Create(() => new BoardActor(typeof(RandomMoveActor), typeof(RandomMoveActor))), "board");
            WhitePlayerName.Text = typeof(RandomMoveActor).GetCustomAttribute<ChessPlayerAttribute>().PlayerName;
            BlackPlayerName.Text = typeof(RandomMoveActor).GetCustomAttribute<ChessPlayerAttribute>().PlayerName;
            PerceivedStrengthsCollection = new SeriesCollection();
            InitializeBoard();
            InitializeStrengthGraph();
            Stop.IsEnabled = false;

            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            var cluster = Akka.Cluster.Cluster.Get(_actorSystem);
            cluster.RegisterOnMemberRemoved(MemberRemoved);
            cluster.Leave(cluster.SelfAddress);

            _actorSystem.WhenTerminated.Wait(TimeSpan.FromSeconds(5));
        }

        private void MemberRemoved()
        {
            _actorSystem.Terminate();
        }

        private void InitializeStrengthGraph()
        {
            _viewModel.PerceivedStrengthHistory = new List<float>();
            PerceivedStrengthsCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "White",
                    Values = new ChartValues<float>(),
                    Fill = Brushes.WhiteSmoke,
                    Stroke = Brushes.White
                },
                new ColumnSeries
                {
                    Title = "Black",
                    Values = new ChartValues<float>(),
                    Fill = Brushes.Black
                }
            };
            
            Labels = new List<string> ();
            Formatter = value => value.ToString("F1");
        }

        private void ResetStrengthGraph()
        {
            PerceivedStrengthsCollection[0].Values.Clear();
            PerceivedStrengthsCollection[1].Values.Clear();
            Labels.Clear();
            _viewModel.PerceivedStrengthHistory.Clear();
        }


        private void InitializeBoard()
        {
            var answer = _board.Ask<GetInitialGameStateAnswer>(new GetInitialGameStateQuestion()).Result;
            _chessBoardState = answer.InitialChessBoardState;
            InitializePlayerList();
            InitializeTiles();

            ChessGridHelper.RemoveAllHighlights(_viewModel.Tiles.Values);
            ChessGridHelper.UpdateUiGrid(_chessBoardState, _viewModel.Tiles.Values.ToList());
            MoveHistoryHelper.SetMoveHistory(_chessBoardState, MoveHistory);
            SetPlayerTime();
            GameFinishedText.Text = "";
            GameFinishedLayout.Visibility = Visibility.Collapsed;
        }

        private async Task InitializeBoardAsync()
        {
            var answer = await _board.Ask<GetInitialGameStateAnswer>(new GetInitialGameStateQuestion());
            _chessBoardState = answer.InitialChessBoardState;
            InitializePlayerList();
            InitializeTiles();
            
            ChessGridHelper.RemoveAllHighlights(_viewModel.Tiles.Values);
            ChessGridHelper.UpdateUiGrid(_chessBoardState, _viewModel.Tiles.Values.ToList());
            MoveHistoryHelper.SetMoveHistory(_chessBoardState, MoveHistory);
            SetPlayerTime();
            GameFinishedText.Text = "";
            GameFinishedLayout.Visibility = Visibility.Collapsed;
        }

        private void InitializeTiles()
        {
            Grid.Children.Clear();
            foreach (var tile in _viewModel.Tiles.Values)
            {
                Grid.Children.Add(new TileUserControl { ViewModel = tile });
            }
        }

        private void InitializePlayerList()
        {
            var players = ChessPlayerHelper.GetPlayers();
            PlayerList.ItemsSource = players;
        }

        private void SetPlayerTime()
        {
            WhitePlayerTime.Text = PlayerTimeHelper.SetPlayerTimeRemaining(_chessBoardState, Player.White);
            BlackPlayerTime.Text = PlayerTimeHelper.SetPlayerTimeRemaining(_chessBoardState, Player.Black);
        }

        private async Task RunGameAsync(bool untilDone)
        {
            if (_isBusy) return;
            _isBusy = true;
            while (true)
            {
                _stopRun = false;
                await MakeMoveAsync();
                ChessGridHelper.UpdateUiGrid(_chessBoardState, _viewModel.Tiles.Values.ToList());
                MoveHistoryHelper.SetMoveHistory(_chessBoardState, MoveHistory);
                SetPlayerTime();
                UpdateStrengthGraph();
                if (untilDone && _chessBoardState.EndResult == null && !_chessBoardState.OutofTime && !_stopRun)
                {
                    continue;
                }
                if (_chessBoardState.EndResult != null || _chessBoardState.OutofTime)
                {
                    GameFinishedText.Text = GetEndResultString(_chessBoardState);
                    GameFinishedLayout.Visibility = Visibility.Visible;
                }
                break;
            }
            _isBusy = false;
        }


        private string GetEndResultString(ChessBoardStateViewModel chessBoardState)
        {
            if (chessBoardState.OutofTime)
            {
                return chessBoardState.OutOfTimeText;
            }
            switch (chessBoardState.EndResult)
            {
                    case StateResult.BlackIsOutOfTime:
                    return $"{_viewModel.WhitePlayer.Name} won! {_viewModel.BlackPlayer.Name} was out of time!";
                case StateResult.BlackKingChecked:
                    return $"{_viewModel.WhitePlayer.Name} won! {_viewModel.BlackPlayer.Name}'s king was checked!???";
                    case StateResult.BlackKingCheckmated:
                    return $"{_viewModel.WhitePlayer.Name} won! {_viewModel.BlackPlayer.Name}'s king was checkmated!";
                case StateResult.BlackIllegalMove:
                    return $"{_viewModel.WhitePlayer.Name} won! {_viewModel.BlackPlayer.Name} attempted an illegal move!";

                    case StateResult.FiftyInconsequentialMoves:
                    return "Remis! Fifty inconsequential moves in a row! (since a piece was taken)";
                    case StateResult.InsufficientMaterial:
                    return "Remis! No player can win from this position.";
                case StateResult.RepeatStateThreeTimes:
                    return "Remis! This position has occured three times.";
                    case StateResult.Stalemate:
                    return "Stalemate! No more legal moves!";
                case StateResult.WhiteIsOutOfTime:
                    return $"{_viewModel.BlackPlayer.Name} won! {_viewModel.WhitePlayer.Name} was out of time!";
                case StateResult.WhiteKingChecked:
                    return $"{_viewModel.BlackPlayer.Name} won! {_viewModel.WhitePlayer.Name}'s king was checked!???";
                case StateResult.WhiteKingCheckmated:
                    return $"{_viewModel.BlackPlayer.Name} won! {_viewModel.WhitePlayer.Name}'s king was checkmated!";
                case StateResult.WhiteIllegalMove:
                    return $"{_viewModel.BlackPlayer.Name} won! {_viewModel.WhitePlayer.Name} attempted an illegal move!";


                default:
                    return "Wh.. What? What just happened?";
            }
        }

        private async Task MakeMoveAsync()
        {
            var answer =  await _board.Ask<GetNextGameStateAnswer>(new GetNextGameStateQuestion());
            _chessBoardState = answer.ChessBoardState;
            _viewModel.PerceivedStrengthHistory.Add(answer.ChessBoardState.LastPerceivedStrength);
        }

        private void UpdateStrengthGraph()
        {
            DataContext = this;
            var value = _viewModel.PerceivedStrengthHistory.Last();
            var roundedValue = Math.Abs(value) < 0.001 ? 0.1f : value;
            if (_viewModel.PerceivedStrengthHistory.Count%2 == 1)
            {
                Labels.Add((_viewModel.PerceivedStrengthHistory.Count / 2 +1).ToString());
                PerceivedStrengthsCollection[0].Values.Add(roundedValue);
            }
            else
            {
                PerceivedStrengthsCollection[1].Values.Add(roundedValue);

                if (PerceivedStrengthsCollection[1].Values.Count > 30)
                {
                    PerceivedStrengthsCollection[1].Values.RemoveAt(0);
                    PerceivedStrengthsCollection[0].Values.RemoveAt(0);
                    Labels.RemoveAt(0);
                }
            }


        }
        
        private ChessPlayerModel ResetGridWithNewPlayer(TextBlock playerName, ChessPlayerModel oppositionPlayer, bool updatingWhite)
        {
            var selected = (ChessPlayerModel)PlayerList.SelectedItem;
            playerName.Text = selected.Name;
            var opposition = oppositionPlayer?.Type ?? typeof(RandomMoveActor);
            _board.GracefulStop(TimeSpan.FromSeconds(5)).Wait();
            _board = _actorSystem.ActorOf(Props.Create(() => new BoardActor(updatingWhite ? selected.Type : opposition, updatingWhite ? opposition : selected.Type)), "board");
            return selected;

        }

        private void EnablePlayButtons(bool enabled)
        {
            var buttons = new[] {SetBlackPlayer, SetWhitePlayer, Run, NextMove, Reset};
            foreach (var button in buttons)
            {
                button.IsEnabled = enabled;
            }
            Stop.IsEnabled = !enabled;
        }

        // Button clicks
        private async void MakeMove_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            EnablePlayButtons(false);
            await RunGameAsync(false);
            EnablePlayButtons(true);
        }

        private void Stop_OnClick(object sender, RoutedEventArgs e)
        {
            _stopRun = true;
        }

        private async void Run_OnClick(object sender, RoutedEventArgs e)
        {
            EnablePlayButtons(false);
            await RunGameAsync(true);
            EnablePlayButtons(true);
        }

        private async void SetWhitePlayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null || _isBusy) return;
            _viewModel.WhitePlayer = ResetGridWithNewPlayer(WhitePlayerName, _viewModel.BlackPlayer, true);
            await InitializeBoardAsync();
            ResetStrengthGraph();
        }

        private async void SetBlackPlayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null || _isBusy) return;
            _viewModel.BlackPlayer = ResetGridWithNewPlayer(BlackPlayerName, _viewModel.WhitePlayer, false);
            await InitializeBoardAsync();
            ResetStrengthGraph();
        }

        private async void ResetBoard_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_isBusy)
            {
                _board.GracefulStop(TimeSpan.FromSeconds(5)).Wait();
                var whiteType = _viewModel.WhitePlayer?.Type ?? typeof(RandomMoveActor);
                var blackType = _viewModel.BlackPlayer?.Type ?? typeof(RandomMoveActor);
                _board = _actorSystem.ActorOf(Props.Create(() => new BoardActor(whiteType, blackType)), "board");
                await InitializeBoardAsync();
                ResetStrengthGraph();
            }
        }
    }
}
