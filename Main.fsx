#load "LWC.fsx"
#load "Buttons.fsx"
open LWC
open Buttons
open System.Windows.Forms
open System.Drawing

//mi definisco la mia nota con le sue caratteristiche
type MyNote(r:Rectangle) as this=
    inherit LWCControl()

    let mutable box = r
    let mutable bgcolor = Color.LightYellow
    let mytitlefont = new Font("Calibri", 16.f)
    let mutable title = "Lorem Ipsum"
    let mutable image = null

    member this.Contains (x, y) =
        box.Contains(x, y)
    
    member this.X
        with get () = box.X
        and set(v) = box <- Rectangle(v, box.Y, box.Width, box.Height)
    member this.Y
        with get () = box.Y
        and set(v) = box <- Rectangle(box.X, v, box.Width, box.Height)
        
    member this.Location //serve per spostare in una sola volta la scatola x,y
        with get() = Point(box.X, box.Y)
        and set(v:Point) = box <- Rectangle (v.X, v.Y, box.Width, box.Height)
    
    member this.FixImage
        with get() = image
        and set(img:Bitmap) = 
            image <- img
    
    member this.FixText
        with get() = title
        and set(str) = 
            title <- str
    
    member this.FixBgcolor
        with get() = bgcolor
        and set(v) = 
            bgcolor <- v
    
    override this.OnPaint(e) =
        let g = e.Graphics //ha come parametro il contesto grafico sul quale disegnerà
        let bkg = e.Graphics.Save()
        use b = new SolidBrush(bgcolor)
        g.SetClip(Rectangle(this.Location.X, this.Location.Y, this.ClientSizeInt.Height, this.ClientSizeInt.Width))
        g.FillRectangle(b, box)
        if image <> null then g.DrawImage(image, RectangleF(PointF(float32 this.Location.X, float32 this.Location.Y),SizeF(this.ClientSize.Height,this.ClientSize.Width)))
        g.DrawString(title, mytitlefont, Brushes.Black, float32 this.Location.X, float32 this.Location.Y)
        e.Graphics.Restore(bkg)

type DrawCanvas() as this =
    inherit Canvas()

    do this.SetStyle(ControlStyles.AllPaintingInWmPaint ||| ControlStyles.OptimizedDoubleBuffer, true)

    let mutable start = None
    let duration = new System.TimeSpan(0,0,0,0,1000)

    let notes = ResizeArray<MyNote>() //è il mio array di note
    let mutable drag = None
    let mutable newnote = None
    
    let mutable lasso = None
    let pointsin = ResizeArray<Point>()

    let timer = new Timer(Interval=100)

    do timer.Tick.Add(fun _ ->
        let easingfunction (start:System.DateTime) (duration:System.TimeSpan) t =
            let dt = t - start
            single(dt.TotalMilliseconds) / single(duration.TotalMilliseconds)

        if start.IsNone then
            start <- Some (System.DateTime.Now)
        
        let perc = easingfunction start.Value duration System.DateTime.Now
        printfn "%A" perc
        if perc >= 1.f  then 
            timer.Stop()
            start <- None
            notes |> Seq.iter (fun b ->
                b.FixBgcolor <- Color.LightYellow
            )
            this.Op <- -1
        
        let linearbezier (x1 : float32) (x2 : float32)  t1 = 
            let  p1 = x1 * (1.f - t1)
            let  p2 = t1 * x2
            p1 + p2
        
        let mutable endpoint = Point()

        notes |> Seq.iter (fun b ->
            if (b.FixBgcolor = Color.Yellow) then 
                endpoint <- b.Location
        )
        
        notes |> Seq.iter (fun b ->
            let x = linearbezier (float32 b.Location.X) (float32 endpoint.X) perc
            let y = linearbezier (float32 b.Location.Y) (float32 endpoint.Y) perc
            if b.FixBgcolor = Color.Yellow then 
                b.Location <- Point((int x), (int y))
        )

        this.Invalidate()
    )

    //let polyvert = Point[]
    let mutable selected = -1

    let mkrect (sx, sy) (ex, ey) = //funzione che mi restituisce un rettangolo dato che posso disegnarlo anche con le prime coordinate in basso a destra e le seconde in altro a sinistra
        Rectangle(min sx ex, min sy ey, abs(sx - ex), abs(sy - ey))

    override this.OnPaint e =
        let g = e.Graphics
        g.SmoothingMode <- Drawing2D.SmoothingMode.HighQuality
        let t = g.Transform
        g.Transform <- this.Mtrasf.WV //mi faccio restituire la matrice dove sono contenuti i controlli per disegnarci sopra
        notes |> Seq.iter (fun b -> //Seq.iter applica a tutti gli elementi dell'array la funzione che gli passo come argomento
            b.OnPaint(e) //come se fosse un for dove chiamo boxes[i].OnPaint e gli passo il contesto grafico
        )
        g.Transform <- t //ripristino la matrice
        base.OnPaint(e)

        match newnote with
        | Some ((sx, sy), (ex, ey)) ->
        let r = mkrect (sx, sy) (ex, ey) //è il rettangolo tratteggiato, infatti non viene aggiunto all'array
        use p = new Pen(Color.Gray)
        p.DashStyle <- Drawing2D.DashStyle.DashDot //tratteggiatura
        g.DrawRectangle(p, r)
        | _ -> ()

        match lasso with
        | Some ((sx, sy), (ex, ey)) ->
        let r = mkrect (sx, sy) (ex, ey)
        use p = new Pen(Color.Red)
        p.DashStyle <- Drawing2D.DashStyle.DashDot
        g.DrawRectangle(p, r)
        | _ -> ()

        //if selected = 1 then g.DrawPolygon(Pens.Red, polyvert)

    override this.OnMouseDown e =
        //il primo passo è fare la Pick correlation
        //let newpoint = TransformPointV (this.Transform.VW) (Point(e.X, e.Y))
        let b = notes |> Seq.tryFindBack (fun box -> box.Contains(e.X, e.Y))
        if (this.Op = -1) then 
            match b with
            | Some box ->
            let dx, dy = e.X - box.X, e.Y - box.Y //offset del click all'interno di box
            drag <- Some (box, dx, dy)
            | _ -> ()
            this.Op <- -1
        if (this.Op = 1) then
            match b with
            | _ -> newnote <- Some ((e.X, e.Y), (e.X, e.Y))
            this.Op <- -1
        if (this.Op = 2) then
            match b with
            | Some box ->
                notes.Remove(box) |> ignore
            | _ -> ()
            this.Op <- -1
        if (this.Op=4) then
            let dlg = new OpenFileDialog()
            dlg.Filter <- "*.JPG|*.JPEG|*.PNG"
            match b with
            | Some box -> 
                if dlg.ShowDialog() = DialogResult.OK then
                    let imagename = dlg.FileName
                    let myPicture : Bitmap = new Bitmap(imagename)
                    box.FixImage <- myPicture
            | _ -> ()
            this.Op <- -1
        if (this.Op=3) then
            let tb = new TextBox(Location=Point(20,300), Width=170, Height=120, Multiline=true)
            let btn1 = new Button(Text="Ok", Location=Point(20,420), Width=85)
            let btn2 = new Button(Text="Fine", Location=Point(105,420), Width=85)
            match b with
            | Some box -> 
                this.Controls.Add(tb)
                this.Controls.Add(btn1)
                this.Controls.Add(btn2)
                tb.Text <- box.FixText
                btn1.Click.Add(fun _ ->
                    box.FixText <- tb.Text
                    this.Invalidate())
                btn2.Click.Add(fun _ ->
                    this.Controls.Remove(tb)
                    this.Controls.Remove(btn1)
                    this.Controls.Remove(btn2)
                    )
            | _ -> ()
            this.Op <- -1
        if (this.Op=5) then
            match lasso with
            | _ -> lasso <- Some ((e.X, e.Y), (e.X, e.Y))
     (* if this.Op=7 then
            polyvert.Add(e.Location)
            printfn "%A" polyvert
            let btn = new Button(Text="FINE Poly", Location=Point(20,420), Width=85)
            this.Controls.Add(btn)
            btn.Click.Add(fun _ ->
                selected <- 1
                this.Controls.Remove(btn)
                this.Invalidate()) *)
        base.OnMouseDown(e)
        //this.Invalidate() ???????

    override this.OnMouseMove e =
        match newnote with
        | Some ((sx, sy), _) ->  //mi mantengo sx,sy e non mi interesso dei punti di arrivo perchè li andrò a modificare
          newnote <- Some((sx, sy), (e.X, e.Y))
          this.Invalidate()
        | _ -> ()
        match drag with
        | Some(box, dx, dy) ->
          box.Location <- Point(e.X - dx, e.Y - dy)
          this.Invalidate()
        | _ -> ()
        match lasso with
        | Some ((sx, sy), _) -> 
          lasso <- Some((sx, sy), (e.X, e.Y))
          this.Invalidate()
        | _ -> ()

    override this.OnMouseUp e =
        match newnote with
        | Some((sx, sy), (ex, ey)) ->
          let rect = mkrect (sx, sy) (ex, ey)
          let r = MyNote(rect)
          r.ClientSize <- SizeF(float32 rect.Height, float32 rect.Width)
          notes.Add(r)
          newnote <- None
          this.Invalidate()
        | _ ->
          drag <- None

        match lasso with
        | Some((sx, sy), (ex, ey)) ->
          let rect = mkrect (sx, sy) (ex, ey)
          notes |> Seq.iter (fun nt -> if rect.Contains(nt.Location) then nt.FixBgcolor <- Color.Yellow)
          lasso <- None
          this.Invalidate()
        | _ -> ()

        if this.Op=6 then
            timer.Start()


let f = new Form(Text="MidTerm: Raffaele Apetino", TopMost=true)
let draw = new DrawCanvas(Dock=DockStyle.Fill)

draw.Invalidate()
f.Controls.Add(draw)
f.MinimumSize <- Size(800,800)
f.Show()