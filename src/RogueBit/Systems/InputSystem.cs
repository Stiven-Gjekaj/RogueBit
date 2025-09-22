using SadConsole.Input;
using SadRogue.Primitives;

namespace RogueBit.Systems;

public class InputSystem
{
    private readonly Game game;
    public InputSystem(Game game) { this.game = game; }

    public void HandleKeyboard(Keyboard keyboard)
    {
        if (game.IsGameOver)
        {
            if (keyboard.IsKeyPressed(Keys.R))
            {
                game.Restart();
                return;
            }
            if (keyboard.IsKeyPressed(Keys.Q))
            {
                game.Quit();
                return;
            }
            return;
        }

        if (keyboard.IsKeyPressed(Keys.Q))
        {
            game.Quit();
            return;
        }
        if (keyboard.IsKeyPressed(Keys.R))
        {
            game.Restart();
            return;
        }

        SadRogue.Primitives.Point? move = null;
        if (keyboard.IsKeyPressed(Keys.Up) || keyboard.IsKeyPressed(Keys.W)) move = new SadRogue.Primitives.Point(0, -1);
        else if (keyboard.IsKeyPressed(Keys.Down) || keyboard.IsKeyPressed(Keys.S)) move = new SadRogue.Primitives.Point(0, 1);
        else if (keyboard.IsKeyPressed(Keys.Left) || keyboard.IsKeyPressed(Keys.A)) move = new SadRogue.Primitives.Point(-1, 0);
        else if (keyboard.IsKeyPressed(Keys.Right) || keyboard.IsKeyPressed(Keys.D)) move = new SadRogue.Primitives.Point(1, 0);

        if (move != null)
        {
            game.HandlePlayerAction(move.Value);
        }
    }
}
