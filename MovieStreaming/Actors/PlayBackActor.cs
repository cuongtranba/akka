using System;
using Akka.Actor;
using MovieStreaming.Messages;

namespace MovieStreaming.Actors
{
    public class PlayBackActor : ReceiveActor
    {
        public PlayBackActor()
        {
            Receive<PlayMovieMessage>(HandlePlayMovieMessage, message => message.UserId == 1);
        }

        protected override void PreStart() => Console.WriteLine("Creating a PlaybackActor");
        protected override void PostStop() => Console.WriteLine("PlayBackActor Stop");
        


        private void HandlePlayMovieMessage(PlayMovieMessage message)
        {
            Console.WriteLine($"Received movie title: {message.MovieTitle}");
            Console.WriteLine($"Received userId: {message.UserId}");
        }

    }
}
