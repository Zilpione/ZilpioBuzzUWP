using NAudio.Wave;
using System.Media;
using System.Text.Json;
using System.Windows.Forms;
namespace ZilpioBuzz
{
	public partial class Form1 : Form
	{
		private const string FileName = "players.json"; // Il nome del file dove salveremo i dati
		private bool ready = false;
		private System.Windows.Forms.Timer timer;
		private int timeLeft;  // Variabile per il tempo (in secondi)
		private int resetTime;
		private int totalTime;
		private Dictionary<string, SoundPlayer> soundPlayers;

		public Form1()
		{
			InitializeComponent();


			LoadPlayerNames();

			// Inizializza il Timer					
			InitializeTimer();

			this.KeyPreview = true;  // Assicura che la form catturi gli eventi KeyDown prima di altri controlli
			this.KeyDown += new KeyEventHandler(Form3_KeyDown);  // Associa l'evento KeyDown al metodo

			Ready();
			// Inizializza il dizionario dei suoni
			soundPlayers = new Dictionary<string, SoundPlayer>();

			LoadLogHistoryFromFile();
			// Precarica i suoni in memoria
			LoadSounds();
		}

		// Inizializza il Timer
		private void InitializeTimer()
		{
			int totalTimeInSeconds;
			if (!int.TryParse(totalTimeTextBox.Text, out totalTimeInSeconds))
			{
				// Se il parsing fallisce, imposta un valore predefinito, ad esempio 30 secondi
				totalTimeInSeconds = 30;
				totalTimeTextBox.Text = "30";
				// Mostra un messaggio di avviso all'utente, se necessario
				//MessageBox.Show("Valore non valido per il tempo totale, è stato impostato a 30 secondi per default.");
			}
			totalTime = totalTimeInSeconds * 10;  // Converti in decimi di secondo
			timeLeft = totalTime;  // Inizializza il tempo rimanente con il totale

			// Ottieni il tempo di reset dal TextBox (in secondi) e converti in decimi di secondo

			int resetTimeInSeconds;
			if (!int.TryParse(resetTimeTextBox.Text, out resetTimeInSeconds))
			{
				// Se il parsing fallisce, imposta un valore predefinito, ad esempio 30 secondi
				resetTimeInSeconds = 10;
				resetTimeTextBox.Text = "10";
				// Mostra un messaggio di avviso all'utente, se necessario
				//MessageBox.Show("Valore non valido per il tempo totale, è stato impostato a 30 secondi per default.");
			}
			resetTime = resetTimeInSeconds * 10;  // Converti in decimi di secondo

			timer = new System.Windows.Forms.Timer();
			timer.Interval = 100;  // Intervallo di 100 millisecondi = 1 decimo di secondo
			timer.Tick += new EventHandler(Timer_Tick);  // Associa l'evento Tick al metodo Timer_Tick

			// Aggiorna subito la Label con il tempo iniziale
			UpdateTimerLabel();
		}
		private int lastSoundSecond = -1;  // Variabile per tenere traccia dell'ultimo secondo in cui è stato riprodotto il suono

		private async void Timer_Tick(object sender, EventArgs e)
		{
			if (timeLeft > 0)
			{
				timeLeft--;  // Decrementa il tempo rimanente (1 decimo di secondo)
				UpdateTimerLabel();  // Aggiorna la Label con il nuovo tempo
									 // Se il tempo è vicino alla fine, riproduci dei suoni
									 // Ottieni i secondi rimanenti
				int secondsLeft = timeLeft;
				//int decimiLeft = timeLeft % 10;

				// Controlla se dobbiamo riprodurre il suono solo una volta per secondo
				if (secondsLeft != lastSoundSecond)
				{
					lastSoundSecond = secondsLeft;
					// Riproduci suoni specifici a determinati intervalli di tempo
					if (secondsLeft == 100)
					{
						await PlayCustomSoundAsync(_ten);  // Riproduci "ding.wav" a 10 secondi
					}
					else if (secondsLeft == 20 || secondsLeft == 10 || secondsLeft == 30)
					{
						await PlayCustomSoundAsync(_bip);  // Riproduci "ding.wav" a 3, 2, 1 secondi
					}

					// Aggiorna il secondo in cui è stato riprodotto il suono

				}
			}
			else
			{
				timer.Stop();  // Ferma il timer se il tempo è scaduto
				EndTimer();
				TimerLabel.Text = "---";

				await PlayCustomSoundAsync(_trombette);
				//LogEvent("----END----");
			}
		}
		private void UpdateTimerLabel()
		{
			// Aggiorna la Label per mostrare i secondi e decimi di secondo
			TimerLabel.Text = GetRemainingTime();

		}
		private string GetRemainingTime()
		{
			// Calcola i secondi e i decimi di secondo rimanenti
			int secondsLeft = timeLeft / 10;  // Secondi rimanenti
			int decimiLeft = timeLeft % 10;   // Decimi di secondo rimanenti
			return $"{secondsLeft}.{decimiLeft} s";
		}
		private async void StopTimer()
		{
			timer.Stop();  // Ferma il timer	
			EndTimer();
			await PlayCustomSoundAsync(_ding);
		}
		private void EndTimer()
		{
			ShowButtons(true);
			LogEvent("-----STOP-----", Color.Red);
		}
		private async void StartTimer()
		{
			if (timeLeft > 0)
			{
				lastPlayerName = "";
				timer.Start();  // Avvia il timer				
				ShowButtons(false);
				LogEvent("------START------", Color.Green);
				await PlayCustomSoundAsync(_bling);
			}
			else
			{
				InitializeTimer();
				StartTimer();

			}
		}
		private void ShowButtons(bool visible)
		{
			// Nasconde o mostra i pulsanti in base al valore di 'visible'
			button1.Visible = visible;  // Pulsante "Save"
			button2.Visible = visible;  // Pulsante "Edit"
			button3.Visible = !ready;  // Pulsante "Ready"
			buttonReset.Visible = visible;  // Pulsante "Reset"
		}
		private string lastPlayerName = "";
		private void Form3_KeyDown(object sender, KeyEventArgs e)
		{
			if (!ready) return;

			// Verifica se uno dei tasti numerici (1-9) o 'i' è stato premuto
			if ((e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) || (e.KeyCode == Keys.I))
			{
				// Calcola l'indice del giocatore basato sul tasto premuto
				int playerIndex = e.KeyCode - Keys.D1 + 1;
				string playerName = GetPlayerNameByIndex(playerIndex, e.KeyCode == Keys.I);
				if (lastPlayerName == playerName) return;
				lastPlayerName = playerName;
				var isBold = false;
				var timeLeftText = GetRemainingTime();
				if (timeLeft > 0 && timeLeft < resetTime)
				{
					timeLeft = resetTime;  // Reset del tempo rimanente al valore di reset
										   //LogEvent($"Il tempo è stato resettato a {resetTime / 10.0} secondi.");
					UpdateTimerLabel();
					isBold = true;
				}
				var color = (timeLeft <= 0 || !timer.Enabled) ? Color.Red : Color.Blue;
				if (!string.IsNullOrEmpty(playerName))
				{
					LogEvent($"{timeLeftText}:{playerName}", color, isBold);
				}
				else
				{
					LogEvent($"{timeLeftText}:{playerIndex}---", color, isBold);
				}

				// Se il tempo rimanente è inferiore al tempo di reset, resettiamo il timer

			}
			else if (e.KeyCode == Keys.D0)  // Numero 0 gestito separatamente
			{
				// Se il timer è attivo, fermalo. Altrimenti, avvialo.
				if (timer.Enabled)
				{
					StopTimer();
				}
				else
				{
					StartTimer();
				}
			}
		}
		private string GetPlayerNameByIndex(int index, bool isI)
		{
			if (isI)
			{
				return Player10TextBox.Text;
			}
			switch (index)
			{
				case 1: return Player1TextBox.Text;
				case 2: return Player2TextBox.Text;
				case 3: return Player3TextBox.Text;
				case 4: return Player4TextBox.Text;
				case 5: return Player5TextBox.Text;
				case 6: return Player6TextBox.Text;
				case 7: return Player7TextBox.Text;
				case 8: return Player8TextBox.Text;
				case 9: return Player9TextBox.Text;
				default: return string.Empty; // Se l'indice non corrisponde a nessun giocatore
			}
		}

		private List<string> logHistory = new List<string>();

		public void LogEvent(string message, Color? color = null, bool isBold = false)
		{
			// Usa il colore di default (nero) se non viene passato un colore specifico
			Color selectedColor = color ?? Color.Black;

			// Salva la posizione attuale per riportarla in cima successivamente
			eventLogTextBox.SelectionStart = 0;  // Imposta il punto di inserimento all'inizio
			eventLogTextBox.SelectionLength = 0;

			// Imposta il colore del testo
			eventLogTextBox.SelectionColor = selectedColor;

			// Imposta il grassetto se isBold è true
			if (isBold)
			{
				eventLogTextBox.SelectionFont = new Font(eventLogTextBox.Font, FontStyle.Bold);
			}
			else
			{
				eventLogTextBox.SelectionFont = new Font(eventLogTextBox.Font, FontStyle.Regular);
			}

			// Inserisci il nuovo messaggio all'inizio del testo
			eventLogTextBox.SelectedText = message + Environment.NewLine;

			// Aggiungi il log alla lista
			logHistory.Add(message);

			// Salva lo storico dei log nel file JSON
			SaveLogHistoryToFile();

			// Ritorna il colore e lo stile al default
			eventLogTextBox.SelectionColor = eventLogTextBox.ForeColor;
			eventLogTextBox.SelectionFont = new Font(eventLogTextBox.Font, FontStyle.Regular);
		}

		private void SaveLogHistoryToFile()
		{
			try
			{
				// Specifica il percorso del file dove salvare lo storico
				string filePath = Path.Combine(Application.StartupPath, "logHistory.json");

				// Serializza la lista dei log in formato JSON
				string json = JsonSerializer.Serialize(logHistory, new JsonSerializerOptions { WriteIndented = true });

				// Scrivi il file JSON su disco
				File.WriteAllText(filePath, json);

				//MessageBox.Show("Lo storico dei log è stato salvato correttamente.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Errore durante il salvataggio dello storico dei log: {ex.Message}");
			}
		}
		/// <summary>
		/// SAVE
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button1_Click(object sender, EventArgs e)
		{
			// Crea un dizionario con i nomi dei giocatori
			var playerNames = new Dictionary<string, string>
		{
			{ "Player1", Player1TextBox.Text },
			{ "Player2", Player2TextBox.Text },
			{ "Player3", Player3TextBox.Text },
			{ "Player4", Player4TextBox.Text },
			{ "Player5", Player5TextBox.Text },
			{ "Player6", Player6TextBox.Text },
			{ "Player7", Player7TextBox.Text },
			{ "Player8", Player8TextBox.Text },
			{ "Player9", Player9TextBox.Text },
			{ "Player10", Player10TextBox.Text },
			{ "TotalTime", totalTimeTextBox.Text },
			{ "ResetTime", resetTimeTextBox.Text }
		};

			// Serializza i dati in JSON e salvali su file
			SavePlayerNames(playerNames);
		}
		private void SavePlayerNames(Dictionary<string, string> playerNames)
		{
			string json = JsonSerializer.Serialize(playerNames);

			// Salva il file nella cartella corrente dell'applicazione
			File.WriteAllText(FileName, json);
			InitializeTimer();
		}
		private void LoadPlayerNames()
		{
			try
			{
				// Controlla se il file esiste
				if (File.Exists(FileName))
				{
					string json = File.ReadAllText(FileName);

					// Deserializza i dati dal JSON
					var playerNames = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

					// Assegna i nomi caricati ai TextBox
					if (playerNames != null)
					{
						Player1TextBox.Text = playerNames.ContainsKey("Player1") ? playerNames["Player1"] : "";
						Player2TextBox.Text = playerNames.ContainsKey("Player2") ? playerNames["Player2"] : "";
						Player3TextBox.Text = playerNames.ContainsKey("Player3") ? playerNames["Player3"] : "";
						Player4TextBox.Text = playerNames.ContainsKey("Player4") ? playerNames["Player4"] : "";
						Player5TextBox.Text = playerNames.ContainsKey("Player5") ? playerNames["Player5"] : "";
						Player6TextBox.Text = playerNames.ContainsKey("Player6") ? playerNames["Player6"] : "";
						Player7TextBox.Text = playerNames.ContainsKey("Player7") ? playerNames["Player7"] : "";
						Player8TextBox.Text = playerNames.ContainsKey("Player8") ? playerNames["Player8"] : "";
						Player9TextBox.Text = playerNames.ContainsKey("Player9") ? playerNames["Player9"] : "";
						Player10TextBox.Text = playerNames.ContainsKey("Player10") ? playerNames["Player10"] : "";

						// Gestisci valori predefiniti e validazione
						totalTimeTextBox.Text = playerNames.ContainsKey("TotalTime") ? playerNames["TotalTime"] : "30";
						resetTimeTextBox.Text = playerNames.ContainsKey("ResetTime") ? playerNames["ResetTime"] : "10";
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Errore durante il caricamento dei nomi: " + ex.Message);
			}
		}

		private void SetTextBoxesReadOnly(bool isReadOnly)
		{
			Player1TextBox.ReadOnly = isReadOnly;
			Player2TextBox.ReadOnly = isReadOnly;
			Player3TextBox.ReadOnly = isReadOnly;
			Player4TextBox.ReadOnly = isReadOnly;
			Player5TextBox.ReadOnly = isReadOnly;
			Player6TextBox.ReadOnly = isReadOnly;
			Player7TextBox.ReadOnly = isReadOnly;
			Player8TextBox.ReadOnly = isReadOnly;
			Player9TextBox.ReadOnly = isReadOnly;
			Player10TextBox.ReadOnly = isReadOnly;
			eventLogTextBox.ReadOnly = isReadOnly;

			totalTimeTextBox.ReadOnly = isReadOnly;
			resetTimeTextBox.ReadOnly = isReadOnly;
		}

		/// <summary>
		/// EDIT
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button2_Click(object sender, EventArgs e)
		{
			ready = false;
			// Abilita l'editing nei TextBox
			SetTextBoxesReadOnly(false);

			// Nasconde il pulsante "Edit" e mostra il pulsante "Pronto"
			button2.Visible = false;
			button3.Visible = true;
		}

		/// <summary>
		/// READY
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button3_Click(object sender, EventArgs e)
		{
			Ready();
		}
		private void Ready()
		{
			// Disabilita l'editing nei TextBox
			SetTextBoxesReadOnly(true);
			ready = true;
			// Nasconde il pulsante "Pronto" e mostra il pulsante "Edit"
			button3.Visible = false;
			button2.Visible = true;

			// Cambia il colore del pulsante "Edit" di nuovo a default (o qualsiasi colore preferito)
			button2.BackColor = SystemColors.Control;  // O usa un colore specifico se vuoi

			// Cambia il colore del pulsante "Pronto" a verde
			button3.BackColor = System.Drawing.Color.Green;

			// Rimuove il focus dal controllo attivo
			this.ActiveControl = null;
		}

		private void buttonReset_Click(object sender, EventArgs e)
		{
			// Reinizializza il timer con i nuovi valori
			InitializeTimer();
			LogEvent("---Reset---");
		}

		string _ding = "ding.wav";
		string _bip = "Bip.wav";
		string _bling = "bling.wav";

		string _ten = "ten.wav";
		string _tree = "tree.wav";
		string _trombette = "trombette.wav";
		//private async Task PlayCustomSoundAsync(string soundFileName)
		//{
		//	try
		//	{
		//		string soundPath = Path.Combine(Application.StartupPath, "Sounds", soundFileName);
		//		SoundPlayer player = new SoundPlayer(soundPath);

		//		await Task.Run(() => player.PlaySync());  // Esegue il suono in modo asincrono su un altro thread
		//	}
		//	catch (Exception ex)
		//	{
		//		MessageBox.Show($"Errore durante la riproduzione del suono: {ex.Message}");
		//	}
		//}
		private async Task PlayCustomSoundAsync(string soundFileName)
		{
			try
			{
				string soundPath = Path.Combine(Application.StartupPath, "Sounds", soundFileName);

				await Task.Run(() =>
				{
					using (var audioFile = new AudioFileReader(soundPath))
					using (var outputDevice = new WaveOutEvent())
					{
						outputDevice.Init(audioFile);
						outputDevice.Play();

						// Aspetta che il suono finisca di suonare
						while (outputDevice.PlaybackState == PlaybackState.Playing)
						{
							System.Threading.Thread.Sleep(100);
						}
					}
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Errore durante la riproduzione del suono: {ex.Message}");
			}
		}
		private void LoadSounds()
		{
			try
			{
				string soundPath = Path.Combine(Application.StartupPath, "Sounds", _ding);
				string soundPath2 = Path.Combine(Application.StartupPath, "Sounds", _bip);
				string soundPath3 = Path.Combine(Application.StartupPath, "Sounds", _bling);
				string soundPath4 = Path.Combine(Application.StartupPath, "Sounds", _ten);
				string soundPath5 = Path.Combine(Application.StartupPath, "Sounds", _trombette);

				SoundPlayer dingPlayer = new SoundPlayer(soundPath);
				SoundPlayer dingPlayer2 = new SoundPlayer(soundPath2);
				SoundPlayer dingPlayer3 = new SoundPlayer(soundPath3);
				SoundPlayer dingPlayer4 = new SoundPlayer(soundPath4);
				SoundPlayer dingPlayer5 = new SoundPlayer(soundPath5);
				dingPlayer.Load();  // Carica il suono in memoria
				dingPlayer2.Load();  // Carica il suono in memoria
				dingPlayer3.Load();  // Carica il suono in memoria
				dingPlayer4.Load();  // Carica il suono in memoria
				dingPlayer5.Load();  // Carica il suono in memoria

				soundPlayers.Add(_ding, dingPlayer);  // Aggiungi il suono caricato al dizionario
				soundPlayers.Add(_bip, dingPlayer2);  // Aggiungi il suono caricato al dizionario
				soundPlayers.Add(_bling, dingPlayer3);  // Aggiungi il suono caricato al dizionario
				soundPlayers.Add(_ten, dingPlayer4);  // Aggiungi il suono caricato al dizionario
				soundPlayers.Add(_trombette, dingPlayer5);  // Aggiungi il suono caricato al dizionario
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Errore durante il caricamento dei suoni: {ex.Message}");
			}
		}
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			// Salva lo storico dei log prima di chiudere
			SaveLogHistoryToFile();
			base.OnFormClosing(e);
		}
		private void LoadLogHistoryFromFile()
		{
			try
			{
				// Specifica il percorso del file dove si trovano i log
				string filePath = Path.Combine(Application.StartupPath, "logHistory.json");

				// Controlla se il file esiste
				if (File.Exists(filePath))
				{
					// Leggi il contenuto del file JSON
					string json = File.ReadAllText(filePath);

					// Deserializza la lista dei log
					logHistory = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

					// Aggiungi ogni log alla RichTextBox
					foreach (var log in logHistory)
					{
						eventLogTextBox.AppendText(log + Environment.NewLine);
					}
				}
				else
				{
					logHistory = new List<string>();  // Se non esiste il file, inizia con una lista vuota
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Errore durante il caricamento dello storico dei log: {ex.Message}");
			}
		}

		private void ClearLogs()
		{
			try
			{
				// Specifica il percorso del file dove si trovano i log
				string oldFilePath = Path.Combine(Application.StartupPath, "logHistory.json");

				// Se il file di log esiste, rinominalo con la data corrente
				if (File.Exists(oldFilePath))
				{
					string newFileName = $"logHistory_{DateTime.Now:yyyyMMdd_HHmmss}.json"; // Nome con data e ora
					string newFilePath = Path.Combine(Application.StartupPath, newFileName);
					File.Move(oldFilePath, newFilePath);  // Rinominare il file
				}

				// Svuota la RichTextBox dei log
				eventLogTextBox.Clear();

				// Svuota la lista dei log
				logHistory.Clear();

				// Crea un nuovo file di log vuoto
				SaveLogHistoryToFile();

				MessageBox.Show("Log cancellati e storico salvato con nome data e ora.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Errore durante la cancellazione e rinomina dei log: {ex.Message}");
			}
		}

		private void button4_Click_1(object sender, EventArgs e)
		{
			ClearLogs();
		}
	}
}