#load "LWC.fsx"
open LWC
open System.Windows.Forms
open System.Drawing

type LWButton() as this=
    inherit LWCControl()

    let mutable op = "null"
    let mutable label = "null"
    let myfont = new Font("Calibri", 8.f)

    member this.Option
        with get() = op
        and set(v) =
            op <- v
    
    member this.name
        with get() = label
        and set(v) = 
            label <- v

    override this.OnPaint(e) =
        let g = e.Graphics
        g.FillRectangle(Brushes.Orange, 0.f , 0.f, 50.f, 30.f)
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
            | _ -> ()
        this.Invalidate()
        base.OnMouseDown(e)

type Canvas() as this =
    inherit LWCContainer()

    let upbutton = new LWButton(name="UP",Position=PointF(80.f,20.f), Option = "up")
    let downbutton = new LWButton(name="DOWN",Position=PointF(80.f,100.f), Option = "down")
    let leftbutton = new LWButton(name="LEFT",Position=PointF(20.f,60.f), Option = "left")
    let rightbutton = new LWButton(name="RIGHT",Position=PointF(140.f,60.f), Option = "right")
    let zoominbutton = new LWButton(name="Z. IN",Position=PointF(20.f,140.f), Option = "zin")
    let zoomoutbutton = new LWButton(name="Z. OUT",Position=PointF(140.f,140.f), Option = "zout")
    let rotatecwbutton = new LWButton(name="R. CW",Position=PointF(20.f,180.f), Option = "rcw")
    let rotateccwbutton = new LWButton(name="R. CCW",Position=PointF(140.f,180.f), Option = "rccw")

    do
        this.LWControls.Add(upbutton)
        this.LWControls.Add(downbutton)
        this.LWControls.Add(leftbutton)
        this.LWControls.Add(rightbutton)
        this.LWControls.Add(zoominbutton)
        this.LWControls.Add(zoomoutbutton)
        this.LWControls.Add(rotatecwbutton)
        this.LWControls.Add(rotateccwbutton)