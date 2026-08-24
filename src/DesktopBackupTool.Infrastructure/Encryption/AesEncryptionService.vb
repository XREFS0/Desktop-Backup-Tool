Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports DesktopBackupTool.Application.Interfaces

Namespace DesktopBackupTool.Infrastructure.Encryption
    Public Class AesEncryptionService
        Implements IEncryptionService

        Private Shared ReadOnly HeaderMagic As Byte() = Encoding.ASCII.GetBytes("DBK1")
        Private Const SaltSize As Integer = 16
        Private Const IvSize As Integer = 16
        Private Const KeySizeBits As Integer = 256
        Private Const Iterations As Integer = 100000

        Public Async Function EncryptFileAsync(sourceFilePath As String, destinationEncryptedPath As String, password As String, cancellationToken As Threading.CancellationToken) As Task Implements IEncryptionService.EncryptFileAsync
            If String.IsNullOrEmpty(password) Then
                Throw New ArgumentException("Password cannot be empty.", NameOf(password))
            End If

            Dim salt(SaltSize - 1) As Byte
            Dim iv(IvSize - 1) As Byte
            RandomNumberGenerator.Fill(salt)
            RandomNumberGenerator.Fill(iv)

            Dim key = DeriveKey(password, salt)
            Dim verifier = CreateVerifier(key)

            Dim destDir = Path.GetDirectoryName(destinationEncryptedPath)
            If Not String.IsNullOrWhiteSpace(destDir) AndAlso Not Directory.Exists(destDir) Then
                Directory.CreateDirectory(destDir)
            End If

            Using outputStream = New FileStream(destinationEncryptedPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync:=True)
                Await outputStream.WriteAsync(HeaderMagic, 0, HeaderMagic.Length, cancellationToken).ConfigureAwait(False)
                Await outputStream.WriteAsync(salt, 0, salt.Length, cancellationToken).ConfigureAwait(False)
                Await outputStream.WriteAsync(iv, 0, iv.Length, cancellationToken).ConfigureAwait(False)
                Await outputStream.WriteAsync(verifier, 0, verifier.Length, cancellationToken).ConfigureAwait(False)

                Using aesAlg = Aes.Create()
                    aesAlg.KeySize = KeySizeBits
                    aesAlg.Key = key
                    aesAlg.IV = iv
                    aesAlg.Mode = CipherMode.CBC
                    aesAlg.Padding = PaddingMode.PKCS7

                    Using encryptor = aesAlg.CreateEncryptor()
                        Using cryptoStream = New CryptoStream(outputStream, encryptor, CryptoStreamMode.Write)
                            Using inputStream = New FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync:=True)
                                Dim buffer(65535) As Byte
                                Dim bytesRead As Integer

                                Do
                                    cancellationToken.ThrowIfCancellationRequested()
                                    bytesRead = Await inputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(False)
                                    If bytesRead > 0 Then
                                        Await cryptoStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(False)
                                    End If
                                Loop While bytesRead > 0

                                Await cryptoStream.FlushFinalBlockAsync(cancellationToken).ConfigureAwait(False)
                            End Using
                        End Using
                    End Using
                End Using
            End Using
        End Function

        Public Async Function DecryptFileAsync(sourceEncryptedPath As String, destinationDecryptedPath As String, password As String, cancellationToken As Threading.CancellationToken) As Task Implements IEncryptionService.DecryptFileAsync
            If String.IsNullOrEmpty(password) Then
                Throw New ArgumentException("Password cannot be empty.", NameOf(password))
            End If

            Dim destDir = Path.GetDirectoryName(destinationDecryptedPath)
            If Not String.IsNullOrWhiteSpace(destDir) AndAlso Not Directory.Exists(destDir) Then
                Directory.CreateDirectory(destDir)
            End If

            Using inputStream = New FileStream(sourceEncryptedPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync:=True)
                Dim magic(HeaderMagic.Length - 1) As Byte
                Dim readMagic = Await inputStream.ReadAsync(magic, 0, magic.Length, cancellationToken).ConfigureAwait(False)
                If readMagic <> magic.Length OrElse Not StructuralComparisons.StructuralEqualityComparer.Equals(magic, HeaderMagic) Then
                    Throw New InvalidDataException("Invalid or unsupported encrypted backup format.")
                End If

                Dim salt(SaltSize - 1) As Byte
                Dim readSalt = Await inputStream.ReadAsync(salt, 0, salt.Length, cancellationToken).ConfigureAwait(False)
                If readSalt <> salt.Length Then
                    Throw New InvalidDataException("Corrupted encrypted file header.")
                End If

                Dim iv(IvSize - 1) As Byte
                Dim readIv = Await inputStream.ReadAsync(iv, 0, iv.Length, cancellationToken).ConfigureAwait(False)
                If readIv <> iv.Length Then
                    Throw New InvalidDataException("Corrupted encrypted file header.")
                End If

                Dim expectedVerifier(31) As Byte
                Dim readVerifier = Await inputStream.ReadAsync(expectedVerifier, 0, expectedVerifier.Length, cancellationToken).ConfigureAwait(False)
                If readVerifier <> expectedVerifier.Length Then
                    Throw New InvalidDataException("Corrupted encrypted file header.")
                End If

                Dim key = DeriveKey(password, salt)
                Dim actualVerifier = CreateVerifier(key)

                If Not CryptographicOperations.FixedTimeEquals(expectedVerifier, actualVerifier) Then
                    Throw New UnauthorizedAccessException("Incorrect password provided for decryption.")
                End If

                Using aesAlg = Aes.Create()
                    aesAlg.KeySize = KeySizeBits
                    aesAlg.Key = key
                    aesAlg.IV = iv
                    aesAlg.Mode = CipherMode.CBC
                    aesAlg.Padding = PaddingMode.PKCS7

                    Using decryptor = aesAlg.CreateDecryptor()
                        Using cryptoStream = New CryptoStream(inputStream, decryptor, CryptoStreamMode.Read)
                            Using outputStream = New FileStream(destinationDecryptedPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync:=True)
                                Dim buffer(65535) As Byte
                                Dim bytesRead As Integer

                                Do
                                    cancellationToken.ThrowIfCancellationRequested()
                                    bytesRead = Await cryptoStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(False)
                                    If bytesRead > 0 Then
                                        Await outputStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(False)
                                    End If
                                Loop While bytesRead > 0
                            End Using
                        End Using
                    End Using
                End Using
            End Using
        End Function

        Public Function GenerateSalt() As String Implements IEncryptionService.GenerateSalt
            Dim salt(SaltSize - 1) As Byte
            RandomNumberGenerator.Fill(salt)
            Return Convert.ToBase64String(salt)
        End Function

        Public Function HashPassword(password As String, saltBase64 As String) As String Implements IEncryptionService.HashPassword
            Dim salt = Convert.FromBase64String(saltBase64)
            Dim key = DeriveKey(password, salt)
            Return Convert.ToBase64String(key)
        End Function

        Public Function VerifyPassword(password As String, saltBase64 As String, expectedHashBase64 As String) As Boolean Implements IEncryptionService.VerifyPassword
            Dim actualHash = HashPassword(password, saltBase64)
            Return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(actualHash), Encoding.UTF8.GetBytes(expectedHashBase64))
        End Function

        Private Function DeriveKey(password As String, salt As Byte()) As Byte()
            Using kdf = New Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256)
                Return kdf.GetBytes(KeySizeBits \ 8)
            End Using
        End Function

        Private Function CreateVerifier(key As Byte()) As Byte()
            Using sha = SHA256.Create()
                Return sha.ComputeHash(key)
            End Using
        End Function
    End Class
End Namespace
