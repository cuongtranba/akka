using System;
using Akka.Actor;
using Akka.Actor.Internal;
using MovieStreaming.Actors;
using MovieStreaming.Messages;

namespace MovieStreaming
{
    class Program
    {
        private static ActorSystem _movieStreamingActorSystem;

        static void Main(string[] args)
        {
            _movieStreamingActorSystem = ActorSystem.Create("MovieStreamingActorSystem");
            Console.WriteLine("Actor system created");

            var playbackActor = Props.Create<PlayBackActor>();

            var playbackActorRef = _movieStreamingActorSystem.ActorOf(playbackActor, "PlayBackActor");

            playbackActorRef.Tell(new PlayMovieMessage("The movie Tile",23));

            Console.ReadLine();
            
        }
    }
}
