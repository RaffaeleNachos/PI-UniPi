//Raffaele Apetino, student @ Unipi: 549220

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
        and set(v) = 
            box <- Rectangle(v, box.Y, box.Width, box.Height)
    member this.Y
        with get () = box.Y
        and set(v) = 
            box <- Rectangle(box.X, v, box.Width, box.Height)
        
    member this.Location
        with get() = Point(box.X, box.Y)
        and set(v:Point) = 
            box <- Rectangle (v.X, v.Y, box.Width, box.Height)
    
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
        let g = e.Graphics
        let bkg = e.Graphics.Save()
        use b = new SolidBrush(bgcolor)
        g.SetClip(Rectangle(this.Location.X, this.Location.Y, this.ClientSizeInt.Height, this.ClientSizeInt.Width))
        g.FillRectangle(b, box)
        if not (isNull image) then g.DrawImage(image, RectangleF(PointF(float32 this.Location.X, float32 this.Location.Y),SizeF(this.ClientSize.Height,this.ClientSize.Width)))
        g.DrawString(title, mytitlefont, Brushes.Black, float32 this.Location.X, float32 this.Location.Y)
        e.Graphics.Restore(bkg)
    
type DrawCanvas() as this =
    inherit Canvas()

    do this.SetStyle(ControlStyles.AllPaintingInWmPaint ||| ControlStyles.OptimizedDoubleBuffer, true)

    //var per timer
    let mutable start = None
    let duration = System.TimeSpan(0,0,0,0,1000)
    let timer = new Timer(Interval=100)

    let notes = ResizeArray<MyNote>() //è il mio array di note
    let mutable drag = None
    let mutable newnote = None
    
    let mutable lasso = None
    
    let raypassingtest numvert (vertarray : ResizeArray<Point>) (pointtest : Point) =
        let mutable i = 0
        let mutable isin = false
        let mutable j = numvert-1
        while i < numvert do 
            if ( ((vertarray.Item(i).Y>pointtest.Y) <> (vertarray.Item(j).Y>pointtest.Y)) && 
                    (pointtest.X < (vertarray.Item(j).X-vertarray.Item(i).X) * (pointtest.Y-vertarray.Item(i).Y) / (vertarray.Item(j).Y-vertarray.Item(i).Y) + vertarray.Item(i).X) ) then
                isin <- not isin
            j <- i
            i <- i+1
        printfn "%A" isin
        isin

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
            //rimetto il bgcolor stock
            notes |> Seq.iter (fun b ->
                b.FixBgcolor <- Color.LightYellow
            )
            this.Op <- -1
        
        //funzione di easing easeInQuart approssimabile con bz
        let easeInQuart (x1 : float32) (x2 : float32)  t1 = 
            let mutable xstart = x1 
            let mutable xend = x2
            xend <- xend - xstart
            let result = (xend * t1 * t1 * t1 * t1) + xstart
            result
        
        let mutable endpoint = Point()

        notes |> Seq.iter (fun b ->
            //il punto di arrivo sarà l'ultima nota nell'array tra le note selezionate 
            if (b.FixBgcolor = Color.Yellow) then 
                endpoint <- b.Location
        )
        
        notes |> Seq.iter (fun b ->
            let x = easeInQuart (float32 b.Location.X) (float32 endpoint.X) perc
            let y = easeInQuart (float32 b.Location.Y) (float32 endpoint.Y) perc
            if b.FixBgcolor = Color.Yellow then 
                b.Location <- Point((int x), (int y))
        )
        this.Invalidate()
    )

    let polyvert = ResizeArray<Point>()
    let mutable numvert = 0
    let mutable selected = -1

    let mkrect (sx, sy) (ex, ey) = //funzione che mi restituisce un rettangolo dato che posso disegnarlo anche con le prime coordinate in basso a destra e le seconde in altro a sinistra
        Rectangle(min sx ex, min sy ey, abs(sx - ex), abs(sy - ey))

    override this.OnPaint e =
        base.OnPaint(e)
        let g = e.Graphics
        g.SmoothingMode <- Drawing2D.SmoothingMode.HighQuality
        let t = g.Transform
        g.Transform <- this.Mtrasf.WV //mi faccio restituire la matrice dove sono contenuti i controlli per disegnarci sopra
        
        notes |> Seq.iter (fun b ->
            b.OnPaint(e) 
        )

        match newnote with
        | Some ((sx, sy), (ex, ey)) ->
        let r = mkrect (sx, sy) (ex, ey) //è il rettangolo tratteggiato, infatti non viene aggiunto all'array
        use p = new Pen(Color.Gray)
        p.DashStyle <- Drawing2D.DashStyle.Dash //tratteggiatura
        g.DrawRectangle(p, r)
        | _ -> ()

        match lasso with
        | Some ((sx, sy), (ex, ey)) ->
        let r = mkrect (sx, sy) (ex, ey)
        use p = new Pen(Color.Red)
        p.DashStyle <- Drawing2D.DashStyle.Dash
        g.DrawRectangle(p, r)
        | _ -> ()

        if selected = 1 then 
            use p = new Pen(Color.Red)
            p.DashStyle <- Drawing2D.DashStyle.Dash
            for i in 0..polyvert.Count-1 do
                g.DrawLine(p, polyvert.Item(i), polyvert.Item((i+1)%(polyvert.Count))) //utilizzo array come memoria circolare e disegno le rette da punto i a punto successivo i+1

        g.Transform <- t //ripristino la matrice

    override this.OnMouseDown e =
        let newpoint = [| (Point(e.X, e.Y)) |] //mi creo il mio array di singolo punto
        this.Mtrasf.VW.TransformPoints(newpoint) //questo casino perchè transformPoints vuole un array, dannati framework
        //sto trasformando il punto da coordinate viste a coordinate mondo ruotate/traslate ecc..
        let b = notes |> Seq.tryFindBack (fun box -> box.Contains(newpoint.[0].X, newpoint.[0].Y))
        if (this.Op = -1) then  //nulla selezionato drag n drop
            match b with
            | Some box ->
                let dx, dy = newpoint.[0].X - box.X, newpoint.[0].Y - box.Y
                drag <- Some (box, dx, dy)
            | _ -> ()
            this.Op <- -1
        if (this.Op = 1) then //nuova nota
            match b with
            | _ -> newnote <- Some ((newpoint.[0].X, newpoint.[0].Y), (newpoint.[0].X, newpoint.[0].Y))
            this.Op <- -1
        if (this.Op = 2) then //elimina nota
            match b with
            | Some box ->
                notes.Remove(box) |> ignore
            | _ -> ()
            this.Op <- -1
        if (this.Op=3) then //aggiungi o modifica testo
            let tb = new TextBox(Location=Point(20,300), Width=170, Height=120, Multiline=true)
            let btn1 = new Button(Text="Ok", Location=Point(20,425), Width=85)
            let btn2 = new Button(Text="Fine", Location=Point(105,425), Width=85)
            let btn3 = new Button(Text="Rimuovi Immagine", Location=Point(20,452), Width=170)
            match b with
            | Some box -> 
                this.Controls.Add(tb)
                this.Controls.Add(btn1)
                this.Controls.Add(btn2)
                this.Controls.Add(btn3)
                tb.Text <- box.FixText
                btn1.Click.Add(fun _ ->
                    box.FixText <- tb.Text
                    this.Invalidate())
                btn2.Click.Add(fun _ ->
                    this.Controls.Remove(tb)
                    this.Controls.Remove(btn1)
                    this.Controls.Remove(btn2)
                    this.Controls.Remove(btn3)
                    )
                btn3.Click.Add(fun _ ->
                    box.FixImage <- null
                    this.Invalidate()
                    )
            | _ -> ()
            this.Op <- -1
        if (this.Op=4) then //aggiungi immagine
            let dlg = new OpenFileDialog()
            dlg.Filter <- "|*.BMP;*.JPG;*.GIF;*.PNG"
            match b with
            | Some box -> 
                if dlg.ShowDialog() = DialogResult.OK then
                    let imagename = dlg.FileName
                    let myPicture : Bitmap = new Bitmap(imagename)
                    box.FixImage <- myPicture
            | _ -> ()
            this.Op <- -1
        if (this.Op=5) then //lasso rettangolare
            match lasso with
            | _ -> lasso <- Some ((newpoint.[0].X, newpoint.[0].Y), (newpoint.[0].X, newpoint.[0].Y))
        if (this.Op=7) then //lasso poligonale
            let btnpoly = new Button(Text="FINE Poly", Location=Point(20,300), Width=85)
            if polyvert.Count = 0 then this.Controls.Add(btnpoly)
            polyvert.Add(Point(newpoint.[0].X, newpoint.[0].Y))
            if polyvert.Count > 1 then selected <- 1 //se ci sono più di 2 punti inizio a disengare il poligono
            btnpoly.Click.Add(fun _ ->
                this.Controls.Remove(btnpoly)
                //chiamo controllo punti interni
                notes |> Seq.iter (fun n ->
                    if ((raypassingtest polyvert.Count polyvert n.Location)) then 
                        n.FixBgcolor <- Color.Yellow
                )
                polyvert.Clear()
                selected <- -1
                this.Op <- -1
                this.Invalidate()
                )
        base.OnMouseDown(e)
        this.Invalidate()

    override this.OnMouseMove e =
        let newpoint = [| (Point(e.X, e.Y)) |] 
        this.Mtrasf.VW.TransformPoints(newpoint)
        match newnote with
        | Some ((sx, sy), _) ->  //mi mantengo sx,sy e non mi interesso dei punti di arrivo perchè li andrò a modificare
          newnote <- Some((sx, sy), (newpoint.[0].X, newpoint.[0].Y))
          this.Invalidate()
        | _ -> ()
        match drag with
        | Some(box, dx, dy) ->
          box.Location <- Point(newpoint.[0].X - dx, newpoint.[0].Y - dy)
          this.Invalidate()
        | _ -> ()
        match lasso with
        | Some ((sx, sy), _) -> 
          lasso <- Some((sx, sy), (newpoint.[0].X, newpoint.[0].Y))
          this.Invalidate()
        | _ -> ()

    override this.OnMouseUp e =
        match newnote with
        | Some((sx, sy), (ex, ey)) ->
          let rect = mkrect (sx, sy) (ex, ey)
          let r = MyNote(rect)
          r.ClientSize <- SizeF(float32 rect.Height, float32 rect.Width)
          notes.Add(r) //aggiungo la nota
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

f.Controls.Add(draw)
f.MinimumSize <- Size(800,800)
f.Show()