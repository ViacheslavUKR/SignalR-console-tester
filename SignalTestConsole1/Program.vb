Imports System
Imports System.IdentityModel.Tokens.Jwt
Imports System.Security.Claims
Imports System.Text
Imports Microsoft.AspNetCore.Http.Connections.Client
Imports Microsoft.AspNetCore.SignalR.Client
Imports Microsoft.Extensions.Configuration
Imports Microsoft.IdentityModel.Tokens
Imports Microsoft.Extensions.Configuration.FileExtensions
Imports Microsoft.Extensions.Configuration.Json
Imports System.IO

Module SignalRClient
    Const URL = "http://localhost:5000/OutputMessages"
    Dim UserID As String = "7f4eeb46-058f-4018-9816-207512604d4e"
    Dim Method As String = "AdminMessages"
    Dim Jwt_Key As String
    Dim Jwt_Issuer As String
    Dim Connection As HubConnection

    Sub Main(args As String())
        Dim Config = New ConfigurationBuilder().
            SetBasePath(Directory.
            GetCurrentDirectory()).
            AddJsonFile("appsettings.json", optional:=True, reloadOnChange:=True).
            Build
        Jwt_Key = Config.GetSection("Jwt:Key").Value
        Jwt_Issuer = Config.GetSection("Jwt:Issuer").Value
        StartClient.Wait()
        Console.ReadKey()
    End Sub

    Async Function StartClient() As Task
        Connection = New HubConnectionBuilder().
                    WithUrl(URL, AddressOf ConnectionOption).
                    WithAutomaticReconnect({TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10)}).
                    Build
        Connection.On(Method, Sub(X) ReceiveMessage(X))
        AddHandler Connection.Closed, AddressOf ConnectionClosed
        Await Connection.StartAsync()
        Console.WriteLine($"Connection {URL}/{Connection.ConnectionId} is {Connection.State}, UserID={UserID}")
    End Function

    Async Sub ConnectionOption(opt As HttpConnectionOptions)
        opt.Headers.Add("Authorization", Await GetJWT(UserID))
    End Sub
    Sub ReceiveMessage(ParamArray X())
        Console.WriteLine($"Recived AdminMessage :{String.Join(",", X)}")
    End Sub

    Async Function ConnectionClosed(x As Exception) As Task
        Console.WriteLine($"ConnectionClosed {x.Message}")
        Await Task.Delay(New Random().Next(0, 5) * 1000)
        Await Connection.StartAsync()
    End Function

    Async Function GetJWT(UserID As String) As Task(Of String)
        Dim EncodedToken As String = ""
        Await Task.Run(Sub()
                           Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = True
                           Dim SecurityKey = New SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwt_Key))
                           Dim Credintals = New SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256)
                           Dim Claims = {New Claim(JwtRegisteredClaimNames.Jti, UserID),
                                         New Claim(ClaimTypes.Role, "None")}
                           Dim Token = New JwtSecurityToken(issuer:=Jwt_Issuer,
                                         audience:=Jwt_Issuer,
                                         Claims,
                                         expires:=DateTime.Now.AddMinutes(30),
                                         signingCredentials:=Credintals)
                           EncodedToken = New JwtSecurityTokenHandler().WriteToken(Token)
                       End Sub)
        Return EncodedToken
    End Function

End Module
