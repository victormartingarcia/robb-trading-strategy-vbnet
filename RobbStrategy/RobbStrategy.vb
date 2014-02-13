Imports System
Imports System.Collections.Generic
Imports TradingMotion.SDK.Algorithms
Imports TradingMotion.SDK.Algorithms.InputParameters
Imports TradingMotion.SDK.Markets.Charts
Imports TradingMotion.SDK.Markets.Orders
Imports TradingMotion.SDK.Markets.Indicators.StatisticFunctions
Imports TradingMotion.SDK.Markets.Indicators.OverlapStudies

Namespace RobbStrategy

    ''' <summary>
    ''' Robb Strategy rules:
    '''     * Entry: Sell when price breaks the lower band of Bollinger Bands indicator
    '''     * Exit: Price hits the Profit target set 3 standard deviations below the entry price
    '''     * Filters: None
    ''' </summary>
    Public Class RobbStrategy
        Inherits Strategy

        Dim bollingerBandsIndicator As BBAndsIndicator
        Dim standardDeviationIndicator As StdDevIndicator

        Public Sub New(ByVal mainChart As Chart, ByVal secondaryCharts As List(Of Chart))
            MyBase.New(mainChart, secondaryCharts)
        End Sub

        ''' <summary>
        ''' Strategy Name
        ''' </summary>
        Public Overrides ReadOnly Property Name As String
            Get
                Return "Robb Strategy"
            End Get
        End Property

        ''' <summary>
        ''' Security filter that ensures the Position will be closed at the end of the trading session.
        ''' </summary>
        Public Overrides ReadOnly Property ForceCloseIntradayPosition As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Security filter that sets a maximum open position size of 1 contract (either side)
        ''' </summary>
        Public Overrides ReadOnly Property MaxOpenPosition As UInteger
            Get
                Return 1
            End Get
        End Property

        ''' <summary>
        ''' This strategy doesn't use the Advanced Order Management mode
        ''' </summary>
        Public Overrides ReadOnly Property UsesAdvancedOrderManagement As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Strategy Parameter definition
        ''' </summary>
        Public Overrides Function SetInputParameters() As InputParameterList

            Dim parameters As New InputParameterList()

            ' The previous N bars period Standard Deviation indicator will use
            parameters.Add(New InputParameter("Standard deviation period", 20))

            ' The distance between the entry price and the profit target order
            parameters.Add(New InputParameter("Profit target standard deviations", 3))

            ' The previous N bars period Bollinger Bands indicator will use
            parameters.Add(New InputParameter("Bollinger Bands period", 58))
            ' The distance between the price and the upper Bollinger band
            parameters.Add(New InputParameter("Upper standard deviations", 3D))
            ' The distance between the price and the lower Bollinger band
            parameters.Add(New InputParameter("Lower standard deviations", 3D))

            Return parameters

        End Function

        ''' <summary>
        ''' Initialization method
        ''' </summary>
        Public Overrides Sub OnInitialize()

            log.Debug("RobbStrategy onInitialize()")

            ' Adding the Standard Deviation indicador to the strategy
            ' (see http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:standard_deviation)
            standardDeviationIndicator = New StdDevIndicator(Me.Bars.Close, Me.GetInputParameter("Standard deviation period"), Me.GetInputParameter("Profit target standard deviations"))
            Me.AddIndicator("Standard Deviation indicator", standardDeviationIndicator)

            ' Adding the Bollinger Bands indicator to the strategy
            ' (see http://www.investopedia.com/terms/b/bollingerbands.asp)
            bollingerBandsIndicator = New BBAndsIndicator(Me.Bars.Close, Me.GetInputParameter("Bollinger Bands period"), Me.GetInputParameter("Upper standard deviations"), Me.GetInputParameter("Lower standard deviations"))
            Me.AddIndicator("Bollinger Bands indicator", bollingerBandsIndicator)

        End Sub

        ''' <summary>
        ''' Strategy enter/exit/filtering rules
        ''' </summary>
        Public Overrides Sub OnNewBar()

            ' Sell signal: the price has crossed above the lower Bollinger band
            If Me.Bars.Close(1) >= bollingerBandsIndicator.GetLowerBand()(1) And Me.Bars.Close(0) < bollingerBandsIndicator.GetLowerBand()(0) And Me.GetOpenPosition() = 0 Then

                ' Entering short and placing a profit target 3 standard deviations below the current market price
                Me.Sell(OrderType.Market, 1, 0D, "Enter short position")
                Me.ExitShort(OrderType.Limit, Me.Bars.Close(0) - standardDeviationIndicator.GetStdDev()(0), "Exit short position (profit target)")

            ElseIf Me.GetOpenPosition() = -1 Then

                ' In Simple Order Management mode, Stop and Limit orders must be placed at every bar
                Me.ExitShort(OrderType.Limit, Me.GetFilledOrders()(0).FillPrice - standardDeviationIndicator.GetStdDev()(0), "Exit short position (profit target)")

            End If

        End Sub

    End Class
End Namespace