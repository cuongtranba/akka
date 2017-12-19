using System;
using System.Security.Cryptography;
using Akka.Actor;
using ColoredConsole;
using MovieStreaming.Messages;

namespace MovieStreaming.Actors
{
    public class UserActor:ReceiveActor
    {
        private string _currenlyWatching;

        public UserActor()
        {
            Console.WriteLine("Creating a UserActor");
            ColorConsole.WriteLine("setting initial behaviour to stopped");
            Stop();
        }

        private void Playing()
        {
            Receive<PlayMovieMessage>(message => ColorConsole.WriteLine("Error: cannot start playing another movie before stopping existing one".Red()));
            Receive<StopMovieMessage>(message => StopPlayingCurrentMovie());

            ColorConsole.WriteLine("UserActor has now become Playing".Cyan());
        }

        private void Stop()
        {
            Receive<PlayMovieMessage>(message => StartPlayMovie(message.MovieTitle));
            Receive<StopMovieMessage>(message => ColorConsole.WriteLine("Error: cannot stop if nothing is playing".Red()));

            ColorConsole.WriteLine("UserActor has now become stopped".Cyan());
        }

        private void StartPlayMovie(string objMovieTitle)
        {
            this._currenlyWatching = objMovieTitle;
            ColorConsole.WriteLine($"User is currently watching {_currenlyWatching}".Yellow());
            Become(Playing);
        }

        private void StopPlayingCurrentMovie()
        {
            ColorConsole.WriteLine($"User has stopped watching {_currenlyWatching}".Yellow());
            _currenlyWatching = null;
            Become(Stop);
        }
    }
}
