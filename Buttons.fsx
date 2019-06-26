#load "LWC.fsx"
open LWC
open System.Windows.Forms
open System.Drawing

let mutable operation = -1

type LWButton() as this=
    inherit LWCControl()

    let mutable op = "null"
    let mutable label = "null"
    let myfont = new Font("Calibri", 8.f)

    member this.Option
        with get() = op
        and set(v) =
            op <- v
    
    member this.Name
        with get() = label
        and set(v) = 
            label <- v

    override this.OnPaint(e) =
        let g = e.Graphics
        g.FillRectangle(Brushes.Orange, 0.f , 0.f, this.ClientSize.Width, this.ClientSize.Height)
        g.DrawString(label, myfont, Brushes.Black, 5.f, 9.f)
    
    override this.OnMouseDown(e) =
        let mutable drawspace = (this :> LWCControl).Parent
        let mutable thematrix = WVMatrix()
        match drawspace with
            | Some p -> thematrix <- p.Mtrasf
            | _ -> ()
        match op with
            | "up" ->
                thematrix.TranslateV(0.f, 10.f)
            | "down" ->
                thematrix.TranslateV(0.f, -10.f)
            | "left" ->
                thematrix.TranslateV(10.f, 0.f)
            | "right" ->
                thematrix.TranslateV(-10.f, 0.f)
            | "zin" ->
                thematrix.ScaleV(1.f/1.1f, 1.f/1.1f)
            | "zout" ->
                thematrix.ScaleV(1.1f, 1.1f)
            | "rcw" ->
                thematrix.RotateV(-10.f)
            | "rccw" ->
                thematrix.RotateV(10.f)
            | "nnote" ->
                operation <- 1
            | "dnote" ->
                operation <- 2
            | "text" ->
                operation <- 3
            | "image" ->
                operation <- 4
            | "rectsel" ->
                operation <- 5
            | "pin" ->
                operation <- 6
            | "polysel" ->
                operation <- 7
            | _ -> ()
        this.Invalidate()
        base.OnMouseDown(e)

type Canvas() as this =
    inherit LWCContainer()

    let upbutton = LWButton(Name="UP",Position=PointF(80.f,20.f), Option = "up")
    let downbutton = LWButton(Name="DOWN",Position=PointF(80.f,100.f), Option = "down")
    let leftbutton = LWButton(Name="LEFT",Position=PointF(20.f,60.f), Option = "left")
    let rightbutton = LWButton(Name="RIGHT",Position=PointF(140.f,60.f), Option = "right")
    let zoominbutton = LWButton(Name="Z. IN",Position=PointF(20.f,140.f), Option = "zin")
    let zoomoutbutton = LWButton(Name="Z. OUT",Position=PointF(140.f,140.f), Option = "zout")
    let rotatecwbutton = LWButton(Name="R. CW",Position=PointF(20.f,180.f), Option = "rcw")
    let rotateccwbutton = LWButton(Name="R. CCW",Position=PointF(140.f,180.f), Option = "rccw")
    let newnote = LWButton(Name="NOTE",Position=PointF(20.f,220.f), Option = "nnote")
    let deletenote = LWButton(Name="DELETE",Position=PointF(20.f,260.f), Option = "dnote")
    let text = LWButton(Name="TEXT",Position=PointF(80.f,220.f), Option = "text")
    let imagenote = LWButton(Name="IMAGE",Position=PointF(80.f,260.f), Option = "image")
    let select = LWButton(Name="SELECT",Position=PointF(140.f,220.f), Option = "rectsel")
    let polyselect = LWButton(Name="POLYSEL",Position=PointF(200.f,220.f), Option = "polysel")
    let pin = LWButton(Name="PIN",Position=PointF(140.f,260.f), Option = "pin")

    do
        this.LWControls.Add(upbutton)
        this.LWControls.Add(downbutton)
        this.LWControls.Add(leftbutton)
        this.LWControls.Add(rightbutton)
        this.LWControls.Add(zoominbutton)
        this.LWControls.Add(zoomoutbutton)
        this.LWControls.Add(rotatecwbutton)
        this.LWControls.Add(rotateccwbutton)
        this.LWControls.Add(newnote)
        this.LWControls.Add(deletenote)
        this.LWControls.Add(text)
        this.LWControls.Add(imagenote)
        this.LWControls.Add(select)
        this.LWControls.Add(pin)
        this.LWControls.Add(polyselect)

    member this.Op
        with get() = operation
        and set(v) = 
            operation <- v