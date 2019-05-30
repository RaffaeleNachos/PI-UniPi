#load "LWC.fsx"
#load "Buttons.fsx"
open LWC
open Buttons
open System.Windows.Forms
open System.Drawing

type DrawObj() as this =
    inherit LWCControl()

    override this.OnPaint(e) = 
        let g = e.Graphics
        g.DrawLine(Pens.Black, 0.f, 0.f, 200.f, 200.f)


type DrawCanvas() as this =
    inherit Canvas()

    override this.OnPaint(e) =
        let g = e.Graphics 
        let t = g.Transform
        g.Transform <- this.Mtrasf.WV //mi faccio restituire la matrice dove sono contenuti i controlli per disegnarci sopra
        g.DrawLine(Pens.Red, 0.f, 0.f, 200.f, 200.f)
        //drawings |> Seq.iter (fun s -> s.OnPaint(e))
        g.Transform <- t //ripristino la matrice
        base.OnPaint(e)

let f = new Form(Text="MidTerm: Raffaele Apetino", TopMost=true)
let draw = new DrawCanvas(Dock=DockStyle.Fill)

draw.Invalidate()
f.Controls.Add(draw)
f.MinimumSize <- Size(800,800)
f.Show()