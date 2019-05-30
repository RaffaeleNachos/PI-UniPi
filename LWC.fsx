open System.Windows.Forms
open System.Drawing

// Libreria

type WVMatrix () =
  let wv = new Drawing2D.Matrix()
  let vw = new Drawing2D.Matrix()

  member this.TranslateW (tx, ty) =
    wv.Translate(tx, ty)
    vw.Translate(-tx, -ty, Drawing2D.MatrixOrder.Append)

  member this.ScaleW (sx, sy) =
    wv.Scale(sx, sy)
    vw.Scale(1.f /sx, 1.f/ sy, Drawing2D.MatrixOrder.Append)

  member this.RotateW (a) =
    wv.Rotate(a)
    vw.Rotate(-a, Drawing2D.MatrixOrder.Append)

  member this.RotateV (a) =
    vw.Rotate(a)
    wv.Rotate(-a, Drawing2D.MatrixOrder.Append)

  member this.TranslateV (tx, ty) =
    vw.Translate(tx, ty)
    wv.Translate(-tx, -ty, Drawing2D.MatrixOrder.Append)

  member this.ScaleV (sx, sy) =
    vw.Scale(sx, sy)
    wv.Scale(1.f /sx, 1.f/ sy, Drawing2D.MatrixOrder.Append)
  
  member this.TransformPointV (p:PointF) =
    let a = [| p |]
    vw.TransformPoints(a)
    a.[0]

  member this.TransformPointW (p:PointF) =
    let a = [| p |]
    wv.TransformPoints(a)
    a.[0]

  member this.VW with get() = vw
  member this.WV with get() = wv

//non vogliamo che la classe LWCControl sia istanziabile
//vogliamo solo che sia la classe padre dei nostri controlli e che abbia certe caratteristiche
//per questo ci sono metodi astratti che servono solo per la segnatura
//riscriviamo delle funzioni che sono già scritte dal sistema grafico
 type LWCControl() =
  let wv = WVMatrix() //mi definisco una matrice propria del controllo

  let mutable sz = SizeF(50.f, 30.f)
  
  let mutable pos = PointF()
  
  let mutable parent : LWCContainer option = None

  member this.WV with get() = wv //metodo che rende la matrice del controllo pubblica (metodo get)

  member this.Parent
    with get() = parent
    and set(v) = parent <- v
  
  abstract OnPaint : PaintEventArgs -> unit //dopo i due punti c'è la segnatura la riscrivo per poter gestire io l'evento
  default this.OnPaint (e) = ()

  abstract OnMouseDown : MouseEventArgs -> unit
  default this.OnMouseDown (e) = ()

  abstract OnMouseUp : MouseEventArgs -> unit
  default this.OnMouseUp (e) = ()

  abstract OnMouseMove : MouseEventArgs -> unit
  default this.OnMouseMove (e) = ()

  member this.Invalidate() = //solo il contenitore LWCContainer sa fare la invalidate
    match parent with
    | Some p -> p.Invalidate()
    | None -> ()

  member this.HitTest(p:Point) = //controllo per la pick correlation, gli passo come parametro il punto arrivato in coordinate vista
    let pt = wv.TransformPointV(PointF(single p.X, single p.Y))
    let boundingbox = RectangleF(0.f, 0.f, sz.Width, sz.Height) //costruisco un rettangolo di appoggio che mi controlla se sono all'interno di esso
    boundingbox.Contains(pt)

  member this.ClientSize //clientsize del nostro controllo
    with get() = sz
    and set(v) = 
      sz <- v
      this.Invalidate()

  member this.Position
    with get() = pos
    and set(v) =
      wv.TranslateV(pos.X, pos.Y) //traslo la vista
      pos <- v //aggiorno la posizione
      wv.TranslateV(-pos.X, -pos.Y) //riporto a posto
      this.Invalidate()

  member this.PositionInt with get() = Point(int pos.X, int pos.Y)  //pos essendo un PointF mi costruisco un metodo che mi restuisce il punto in int
  member this.ClientSizeInt with get() = Size(int sz.Width, int sz.Height)

  member this.Left = pos.X
  member this.Top = pos.Y
  member this.Width = sz.Width
  member this.Height = sz.Height

and
  LWCContainer() as this=
  inherit UserControl()

  let controls = System.Collections.ObjectModel.ObservableCollection<LWCControl>()  //è una collezione osservabile che quando cambia lancia un evento
  //la collezione osservabile però contiene tipi obj

  let mydrawmatrix = WVMatrix()

  do 
    controls.CollectionChanged.Add(fun e ->
      for i in e.NewItems do
        (i :?> LWCControl).Parent <- Some(this :?> LWCContainer)
    )

  member this.LWControls with get() = controls //metodo che restituisce l'array di controlli
  member this.Mtrasf with get() = mydrawmatrix

  override this.OnMouseDown (e) = //pick correlation sul container che andrà a controllare nell'array su quale LWControl sto cliccando
    let oc =
      controls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location)) //chiamo la hit test contenuta in LWCControl
    match oc with
    | Some c -> 
      let p = c.WV.TransformPointV(PointF(single e.X, single e.Y)) //punto da coordinate vista a coordinate mondo
      let evt = new MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta) //delta è la rotazione della rotella del mouse
      c.OnMouseDown(evt)
    | None -> () 

  override this.OnMouseUp (e) =
    let oc =
      controls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location))
    match oc with
    | Some c ->
      let p = c.WV.TransformPointV(PointF(single e.X, single e.Y))
      let evt = new MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
      c.OnMouseUp(evt)
    | None -> () 

  override this.OnMouseMove (e) =
    let oc =
      controls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location))
    match oc with
    | Some c ->
      let p = c.WV.TransformPointV(PointF(single e.X, single e.Y))
      let evt = new MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
      c.OnMouseMove(evt)
    | None -> () 

  override this.OnPaint(e) =
    controls |> Seq.iter(fun c ->
      let bkg = e.Graphics.Save() //mi salvo il contesto grafico
      let evt = new PaintEventArgs(e.Graphics, Rectangle(c.PositionInt, c.ClientSizeInt))
      e.Graphics.SetClip(new RectangleF(c.Position,c.ClientSize)) //non supporta la rotazione!
      e.Graphics.Transform <- c.WV.WV //utilizza il metodo get per farmi restituire la matrice del controllo
      c.OnPaint(evt) //poi ci disegno sopra
      e.Graphics.Restore(bkg) //ripristino lo stato del contesto grafico
    )
  
  override this.OnKeyDown e =
    match e.KeyCode with
    | Keys.W -> 
      mydrawmatrix.TranslateV(0.f, 10.f)
    | Keys.A -> 
      mydrawmatrix.TranslateV(10.f, 0.f)
    | Keys.S -> 
      mydrawmatrix.TranslateV(0.f, -10.f)
    | Keys.D ->
      mydrawmatrix.TranslateV(-10.f, 0.f)
    | Keys.Q ->
      mydrawmatrix.RotateV(10.f)
    | Keys.E ->
      mydrawmatrix.RotateV(-10.f)
    | Keys.Z ->
      mydrawmatrix.ScaleV(1.1f, 1.1f)
    | Keys.X ->
      mydrawmatrix.ScaleV(1.f/1.1f, 1.f/1.1f)
    | _ -> ()
    this.Invalidate()