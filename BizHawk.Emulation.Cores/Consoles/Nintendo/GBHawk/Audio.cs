﻿using System;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Audio Emulation
	public class Audio : ISoundProvider
	{
		public GBHawk Core { get; set; }

		public static int[] DUTY_CYCLES = new int[] {0, 0, 0, 0, 0, 0, 0, 1,
													 1, 0, 0, 0, 0, 0, 0, 1,
													 1, 0, 0, 0, 0, 1, 1, 1,
													 0, 1, 1, 1, 1, 1, 1, 0};

		public static int[] DIVISOR = new int[] {8, 16, 32, 48, 64, 80, 96, 112};


		public const int NR10 = 0;
		public const int NR11 = 1;
		public const int NR12 = 2;
		public const int NR13 = 3;
		public const int NR14 = 4;
		public const int NR21 = 5;
		public const int NR22 = 6;
		public const int NR23 = 7;
		public const int NR24 = 8;
		public const int NR30 = 9;
		public const int NR31 = 10;
		public const int NR32 = 11;
		public const int NR33 = 12;
		public const int NR34 = 13;
		public const int NR41 = 14;
		public const int NR42 = 15;
		public const int NR43 = 16;
		public const int NR44 = 17;
		public const int NR50 = 18;
		public const int NR51 = 19;
		public const int NR52 = 20;

		public static int[] unused_bits = new int[] { 0x80, 0x3F, 0x00, 0xFF, 0xBF,
															0x3F, 0x00, 0xFF, 0xBF,
													  0x7F, 0xFF, 0x9F, 0xFF, 0xBF,
															0xFF, 0x00, 0x00, 0xBF,
													  0x00, 0x00, 0x70};

		public byte[] Audio_Regs = new byte[21];

		public byte[] Wave_RAM = new byte [16];


		// Audio Variables
		// derived
		public bool											WAVE_DAC_pow;
		public bool																	NOISE_wdth_md;
		public bool SQ1_negate;
		public bool SQ1_trigger,		SQ2_trigger,		WAVE_trigger,			NOISE_trigger;
		public bool SQ1_len_en,			SQ2_len_en,			WAVE_len_en,			NOISE_len_en;
		public bool SQ1_env_add,		SQ2_env_add,								NOISE_env_add;
		public byte											WAVE_vol_code;
		public byte																	NOISE_clk_shft;
		public byte																	NOISE_div_code;
		public byte SQ1_shift;
		public byte SQ1_duty,			SQ2_duty;		
		public byte SQ1_st_vol,			SQ2_st_vol,									NOISE_st_vol;		
		public byte SQ1_per,			SQ2_per,									NOISE_per;
		public byte SQ1_swp_prd;
		public int SQ1_frq,				SQ2_frq,			WAVE_frq;
		public ushort SQ1_length,		SQ2_length,			WAVE_length,			NOISE_length;
		// state
		public bool SQ1_enable,			SQ2_enable,			WAVE_enable,			NOISE_enable;
		public byte SQ1_vol_state,		SQ2_vol_state,								NOISE_vol_state;
		public byte SQ1_duty_cntr,		SQ2_duty_cntr;
		public byte											WAVE_wave_cntr;
		public int SQ1_frq_shadow;
		public int SQ1_intl_cntr,		SQ2_intl_cntr,		WAVE_intl_cntr,			NOISE_intl_cntr;		
		public int SQ1_vol_per,			SQ2_vol_per,								NOISE_vol_per;
		public int SQ1_intl_swp_cnt;
		public int																	NOISE_LFSR;
		public ushort SQ1_len_cntr,		SQ2_len_cntr,		WAVE_len_cntr,			NOISE_len_cntr;
		// computed
		public int SQ1_output,			SQ2_output,			WAVE_output,			NOISE_output;

		// Contol Variables
		public bool AUD_CTRL_vin_L_en;
		public bool AUD_CTRL_vin_R_en;
		public bool AUD_CTRL_sq1_L_en;
		public bool AUD_CTRL_sq2_L_en;
		public bool AUD_CTRL_wave_L_en;
		public bool AUD_CTRL_noise_L_en;
		public bool AUD_CTRL_sq1_R_en;
		public bool AUD_CTRL_sq2_R_en;
		public bool AUD_CTRL_wave_R_en;
		public bool AUD_CTRL_noise_R_en;
		public bool AUD_CTRL_power;
		public byte AUD_CTRL_vol_L;
		public byte AUD_CTRL_vol_R;


		public int sequencer_len, sequencer_vol, sequencer_swp, sequencer_tick;

		public int master_audio_clock;

		public byte ReadReg(int addr)
		{
			byte ret = 0;

			switch (addr)
			{		
				case 0xFF10: ret = (byte)(Audio_Regs[NR10] | unused_bits[NR10]);		break; // NR10 (sweep)
				case 0xFF11: ret = (byte)(Audio_Regs[NR11] | unused_bits[NR11]);		break; // NR11 (sound length / wave pattern duty %)
				case 0xFF12: ret = (byte)(Audio_Regs[NR12] | unused_bits[NR12]);		break; // NR12 (envelope)
				case 0xFF13: ret = (byte)(Audio_Regs[NR13] | unused_bits[NR13]);		break; // NR13 (freq low)
				case 0xFF14: ret = (byte)(Audio_Regs[NR14] | unused_bits[NR14]);		break; // NR14 (freq hi)
				case 0xFF16: ret = (byte)(Audio_Regs[NR21] | unused_bits[NR21]);		break; // NR21 (sound length / wave pattern duty %)
				case 0xFF17: ret = (byte)(Audio_Regs[NR22] | unused_bits[NR22]);		break; // NR22 (envelope)
				case 0xFF18: ret = (byte)(Audio_Regs[NR23] | unused_bits[NR23]);		break; // NR23 (freq low)
				case 0xFF19: ret = (byte)(Audio_Regs[NR24] | unused_bits[NR24]);		break; // NR24 (freq hi)
				case 0xFF1A: ret = (byte)(Audio_Regs[NR30] | unused_bits[NR30]);		break; // NR30 (on/off)
				case 0xFF1B: ret = (byte)(Audio_Regs[NR31] | unused_bits[NR31]);		break; // NR31 (length)
				case 0xFF1C: ret = (byte)(Audio_Regs[NR32] | unused_bits[NR32]);		break; // NR32 (level output)
				case 0xFF1D: ret = (byte)(Audio_Regs[NR33] | unused_bits[NR33]);		break; // NR33 (freq low)
				case 0xFF1E: ret = (byte)(Audio_Regs[NR34] | unused_bits[NR34]);		break; // NR34 (freq hi)
				case 0xFF20: ret = (byte)(Audio_Regs[NR41] | unused_bits[NR41]);		break; // NR41 (length)
				case 0xFF21: ret = (byte)(Audio_Regs[NR42] | unused_bits[NR42]);		break; // NR42 (envelope)
				case 0xFF22: ret = (byte)(Audio_Regs[NR43] | unused_bits[NR43]);		break; // NR43 (shift)
				case 0xFF23: ret = (byte)(Audio_Regs[NR44] | unused_bits[NR44]);		break; // NR44 (trigger)
				case 0xFF24: ret = (byte)(Audio_Regs[NR50] | unused_bits[NR50]);		break; // NR50 (ctrl)
				case 0xFF25: ret = (byte)(Audio_Regs[NR51] | unused_bits[NR51]);		break; // NR51 (ctrl)
				case 0xFF26: ret = (byte)(Audio_Regs[NR52] | unused_bits[NR52]);	Console.WriteLine(Audio_Regs[NR52] & 0xF);	break; // NR52 (ctrl)

				// wave ram table
				case 0xFF30:
				case 0xFF31:
				case 0xFF32:
				case 0xFF33:
				case 0xFF34:
				case 0xFF35:
				case 0xFF36:
				case 0xFF37:
				case 0xFF38:
				case 0xFF39:
				case 0xFF3A:
				case 0xFF3B:
				case 0xFF3C:
				case 0xFF3D:
				case 0xFF3E:
				case 0xFF3F:
					ret = Wave_RAM[addr & 0x0F];
					break;

			}

			return ret;
		}

		public void WriteReg(int addr, byte value)
		{
			// while power is on, everything is writable
			if (AUD_CTRL_power)
			{
				switch (addr)
				{
					case 0xFF10:                                        // NR10 (sweep)
						Audio_Regs[NR10] = value;
						SQ1_swp_prd = (byte)((value & 0x70) >> 4);
						SQ1_negate = (value & 8) > 0;
						SQ1_shift = (byte)(value & 7);
						if (SQ1_swp_prd == 0) { SQ1_swp_prd = 8; }
						break;
					case 0xFF11:                                        // NR11 (sound length / wave pattern duty %)
						Audio_Regs[NR11] = value;
						SQ1_duty = (byte)((value & 0xC0) >> 6);
						SQ1_length = (ushort)(64 - value & 0x3F);
						SQ1_len_cntr = SQ1_length;
						break;
					case 0xFF12:                                        // NR12 (envelope)
						Audio_Regs[NR12] = value;
						SQ1_st_vol = (byte)((value & 0xF0) >> 4);
						SQ1_env_add = (value & 8) > 0;
						SQ1_per = (byte)(value & 7);
						if (SQ1_per == 0) { SQ1_per = 8; }
						break;
					case 0xFF13:                                        // NR13 (freq low)
						Audio_Regs[NR13] = value;
						SQ1_frq &= 0x700;
						SQ1_frq |= value;
						break;
					case 0xFF14:                                        // NR14 (freq hi)
						Audio_Regs[NR14] = value;
						SQ1_trigger = (value & 0x80) > 0;
						SQ1_len_en = (value & 0x40) > 0;
						SQ1_frq &= 0xFF;
						SQ1_frq |= (ushort)((value & 7) << 8);

						if (SQ1_trigger)
						{
							SQ1_enable = true;
							Audio_Regs[NR52] |= 1;
							if (SQ1_len_cntr == 0) { SQ1_len_cntr = 64; }
							SQ1_intl_cntr = 0;
							SQ1_vol_state = SQ1_st_vol;
							SQ1_vol_per = 0;
							SQ1_frq_shadow = SQ1_frq;
							if ((SQ1_vol_state == 0) && !SQ1_env_add) { SQ1_enable = false; Audio_Regs[NR52] &= 0xFE; }
						}
						break;
					case 0xFF16:                                        // NR21 (sound length / wave pattern duty %)		
						Audio_Regs[NR21] = value;
						SQ2_duty = (byte)((value & 0xC0) >> 6);
						SQ2_length = (ushort)(64 - value & 0x3F);
						SQ2_len_cntr = SQ2_length;
						break;
					case 0xFF17:                                        // NR22 (envelope)
						Audio_Regs[NR22] = value;
						SQ2_st_vol = (byte)((value & 0xF0) >> 4);
						SQ2_env_add = (value & 8) > 0;
						SQ2_per = (byte)(value & 7);
						//if (SQ2_per == 0) { SQ2_per = 8; }
						break;
					case 0xFF18:                                        // NR23 (freq low)
						Audio_Regs[NR23] = value;
						SQ2_frq &= 0x700;
						SQ2_frq |= value;
						break;
					case 0xFF19:                                        // NR24 (freq hi)
						Audio_Regs[NR24] = value;
						SQ2_trigger = (value & 0x80) > 0;
						SQ2_len_en = (value & 0x40) > 0;
						SQ2_frq &= 0xFF;
						SQ2_frq |= (ushort)((value & 7) << 8);

						if (SQ2_trigger)
						{
							SQ2_enable = true;
							Audio_Regs[NR52] |= 2;
							if (SQ2_len_cntr == 0) { SQ2_len_cntr = 64; }
							SQ2_intl_cntr = 0;
							SQ2_vol_state = SQ2_st_vol;
							SQ2_vol_per = 0;
							if ((SQ2_vol_state == 0) && !SQ2_env_add) { SQ2_enable = false; Audio_Regs[NR52] &= 0xFD; }
						}
						break;
					case 0xFF1A:                                        // NR30 (on/off)
						Audio_Regs[NR30] = value;
						WAVE_DAC_pow = (value & 0x80) > 0;
						break;
					case 0xFF1B:                                        // NR31 (length)
						Audio_Regs[NR31] = value;
						WAVE_length = (ushort)(256 - value);
						WAVE_len_cntr = WAVE_length;
						break;
					case 0xFF1C:                                        // NR32 (level output)
						Audio_Regs[NR32] = value;
						WAVE_vol_code = (byte)((value & 0x60) >> 5);
						break;
					case 0xFF1D:                                        // NR33 (freq low)
						Audio_Regs[NR33] = value;
						WAVE_frq &= 0x700;
						WAVE_frq |= value;
						break;
					case 0xFF1E:                                        // NR34 (freq hi)
						Audio_Regs[NR34] = value;
						WAVE_trigger = (value & 0x80) > 0;
						WAVE_len_en = (value & 0x40) > 0;
						WAVE_frq &= 0xFF;
						WAVE_frq |= (ushort)((value & 7) << 8);

						if (WAVE_trigger)
						{
							WAVE_enable = true;
							Audio_Regs[NR52] |= 4;
							if (WAVE_len_cntr == 0) { WAVE_len_cntr = 256; }
							WAVE_intl_cntr = 0;
							WAVE_wave_cntr = 0;
							if (!WAVE_DAC_pow) { WAVE_enable = false; Audio_Regs[NR52] &= 0xFB; }
						}
						break;
					case 0xFF20:                                        // NR41 (length)
						Audio_Regs[NR41] = value;
						NOISE_length = (ushort)(64 - value & 0x3F);
						NOISE_len_cntr = NOISE_length;
						break;
					case 0xFF21:                                        // NR42 (envelope)
						Audio_Regs[NR42] = value;
						NOISE_st_vol = (byte)((value & 0xF0) >> 4);
						NOISE_env_add = (value & 8) > 0;
						NOISE_per = (byte)(value & 7);
						//if (NOISE_per == 0) { NOISE_per = 8; }
						break;
					case 0xFF22:                                        // NR43 (shift)
						Audio_Regs[NR43] = value;
						NOISE_clk_shft = (byte)((value & 0xF0) >> 4);
						NOISE_wdth_md = (value & 8) > 0;
						NOISE_div_code = (byte)(value & 7);
						break;
					case 0xFF23:                                        // NR44 (trigger)
						Audio_Regs[NR44] = value;
						NOISE_trigger = (value & 0x80) > 0;
						NOISE_len_en = (value & 0x40) > 0;

						if (NOISE_trigger)
						{
							NOISE_enable = true;
							Audio_Regs[NR52] |= 8;
							if (NOISE_len_cntr == 0) { NOISE_len_cntr = 64; }
							NOISE_intl_cntr = 0;
							NOISE_vol_state = NOISE_st_vol;
							NOISE_vol_per = 0;
							NOISE_LFSR = 0x7FFF;
							if ((NOISE_vol_state == 0) && !NOISE_env_add) { NOISE_enable = false; Audio_Regs[NR52] &= 0xF7; }
						}
						break;
					case 0xFF24:                                        // NR50 (ctrl)
						Audio_Regs[NR50] = value;
						AUD_CTRL_vin_L_en = (value & 0x80) > 0;
						AUD_CTRL_vol_L = (byte)((value & 0x70) >> 4);
						AUD_CTRL_vin_R_en = (value & 8) > 0;
						AUD_CTRL_vol_R = (byte)(value & 7);
						break;
					case 0xFF25:                                        // NR51 (ctrl)
						Audio_Regs[NR51] = value;
						AUD_CTRL_noise_L_en = (value & 0x80) > 0;
						AUD_CTRL_wave_L_en = (value & 0x40) > 0;
						AUD_CTRL_sq2_L_en = (value & 0x20) > 0;
						AUD_CTRL_sq1_L_en = (value & 0x10) > 0;
						AUD_CTRL_noise_R_en = (value & 8) > 0;
						AUD_CTRL_wave_R_en = (value & 4) > 0;
						AUD_CTRL_sq2_R_en = (value & 2) > 0;
						AUD_CTRL_sq1_R_en = (value & 1) > 0;
						break;
					case 0xFF26:                                        // NR52 (ctrl)
						Audio_Regs[NR52] &= 0x7F;
						Audio_Regs[NR52] |= (byte)(value & 0x80);
						AUD_CTRL_power = (value & 0x80) > 0;

						if (!AUD_CTRL_power)
						{
							power_off();
						}
						break;

					// wave ram table
					case 0xFF30:
					case 0xFF31:
					case 0xFF32:
					case 0xFF33:
					case 0xFF34:
					case 0xFF35:
					case 0xFF36:
					case 0xFF37:
					case 0xFF38:
					case 0xFF39:
					case 0xFF3A:
					case 0xFF3B:
					case 0xFF3C:
					case 0xFF3D:
					case 0xFF3E:
					case 0xFF3F:
						Wave_RAM[addr & 0x0F] = value;
						break;
				}
			}
			// when power is off, only length counters and waveRAM are effected by writes
			else
			{
				switch (addr)
				{
					case 0xFF11:                                        // NR11 (sound length / wave pattern duty %)
						SQ1_length = (ushort)(64 - value & 0x3F);
						break;
					case 0xFF16:                                        // NR21 (sound length / wave pattern duty %)		
						SQ2_length = (ushort)(64 - value & 0x3F);
						break;
					case 0xFF1B:                                        // NR31 (length)
						WAVE_length = (ushort)(256 - value);
						break;
					case 0xFF20:                                        // NR41 (length)
						NOISE_length = (ushort)(64 - value & 0x3F);
						break;
					case 0xFF26:                                        // NR52 (ctrl)
						Audio_Regs[NR52] &= 0x7F;
						Audio_Regs[NR52] |= (byte)(value & 0x80);
						AUD_CTRL_power = (value & 0x80) > 0;
						break;

					// wave ram table
					case 0xFF30:
					case 0xFF31:
					case 0xFF32:
					case 0xFF33:
					case 0xFF34:
					case 0xFF35:
					case 0xFF36:
					case 0xFF37:
					case 0xFF38:
					case 0xFF39:
					case 0xFF3A:
					case 0xFF3B:
					case 0xFF3C:
					case 0xFF3D:
					case 0xFF3E:
					case 0xFF3F:
						Wave_RAM[addr & 0x0F] = value;
						break;
				}
			}
		}

		public void tick()
		{
			// calculate square1's output
			if (SQ1_enable)
			{
				SQ1_intl_cntr++;
				if (SQ1_intl_cntr >= (2048 - SQ1_frq_shadow) * 4)
				{
					SQ1_intl_cntr = 0;
					SQ1_duty_cntr++;
					SQ1_duty_cntr &= 7;

					SQ1_output = DUTY_CYCLES[SQ1_duty * 8 + SQ1_duty_cntr];
					SQ1_output *= SQ1_vol_state;
				}
			}

			// calculate square2's output
			if (SQ2_enable)
			{
				SQ2_intl_cntr++;
				if (SQ2_intl_cntr >= (2048 - SQ2_frq) * 4)
				{
					SQ2_intl_cntr = 0;
					SQ2_duty_cntr++;
					SQ2_duty_cntr &= 7;

					SQ2_output = DUTY_CYCLES[SQ2_duty * 8 + SQ2_duty_cntr];
					SQ2_output *= SQ2_vol_state;
				}
			}

			// calculate wave output
			if (WAVE_enable)
			{
				WAVE_intl_cntr++;
				if (WAVE_intl_cntr >= (2048 - WAVE_frq) * 2)
				{
					WAVE_intl_cntr = 0;
					WAVE_wave_cntr++;
					WAVE_wave_cntr &= 0x1F;

					byte sample = Wave_RAM[WAVE_wave_cntr >> 1];

					if ((WAVE_wave_cntr & 1) == 0)
					{
						sample = (byte)(sample >> 4);
					}

					if (WAVE_vol_code == 0)
					{
						sample = (byte)((sample & 0xF) >> 4);
					}
					else if (WAVE_vol_code == 1)
					{
						sample = (byte)(sample & 0xF);
					}
					else if (WAVE_vol_code == 2)
					{
						sample = (byte)((sample & 0xF) >> 1);
					}
					else
					{
						sample = (byte)((sample & 0xF) >> 2);
					}

					WAVE_output = sample;

					if (!WAVE_DAC_pow)
					{
						WAVE_output = 0;
					}
				}
			}


			// calculate noise output
			if (NOISE_enable)
			{
				NOISE_intl_cntr++;
				if (NOISE_intl_cntr >= (DIVISOR[NOISE_div_code] << NOISE_clk_shft))
				{
					NOISE_intl_cntr = 0;
					int bit_lfsr = (NOISE_LFSR & 1) ^ ((NOISE_LFSR & 2) >> 1);

					NOISE_LFSR = (NOISE_LFSR >> 1) & 0x3FFF;
					NOISE_LFSR |= (bit_lfsr << 14);

					if (NOISE_wdth_md)
					{
						NOISE_LFSR = NOISE_LFSR & 0x7FBF;
						NOISE_LFSR |= (bit_lfsr << 6);
					}

					NOISE_output = NOISE_LFSR & 1;
					NOISE_output *= NOISE_vol_state;
				}
			}

			// add up components to each channel
			int L_final = 0;
			int R_final = 0;

			if (AUD_CTRL_sq1_L_en) { L_final += SQ1_output; }
			if (AUD_CTRL_sq2_L_en) { L_final += SQ2_output; }
			if (AUD_CTRL_wave_L_en) { L_final += WAVE_output; }
			if (AUD_CTRL_noise_L_en) { L_final += NOISE_output; }

			if (AUD_CTRL_sq1_R_en) { R_final += SQ1_output; }
			if (AUD_CTRL_sq2_R_en) { R_final += SQ2_output; }
			if (AUD_CTRL_wave_R_en) { R_final += WAVE_output; }
			if (AUD_CTRL_noise_R_en) { R_final += NOISE_output; }

			L_final *= (AUD_CTRL_vol_L + 1);
			R_final *= (AUD_CTRL_vol_R + 1);

			// send out an actual sample every 94 cycles
			master_audio_clock++;
			if (master_audio_clock == 94)
			{
				master_audio_clock = 0;
				if (AudioClocks < 1500)
				{
					AudioSamples[AudioClocks] = (short)(L_final * 4);
					/*
					Console_Write(SQ1_output);
					Console_Write(" ");
					Console_Write(SQ2_output);
					Console_Write(" ");
					Console_Write(WAVE_output);
					Console_Write(" ");
					Console_WriteLine(NOISE_output);
					*/
					AudioClocks++;
					AudioSamples[AudioClocks] = (short)(R_final * 4);
					AudioClocks++;
				}
			}

			// frame sequencer ticks at a rate of 512 hz (or every time a 13 bit counter rolls over)
			sequencer_tick++;

			if (sequencer_tick==8192)
			{
				sequencer_tick = 0;

				sequencer_vol++; sequencer_vol &= 0x7;
				sequencer_len++; sequencer_len &= 0x7;
				sequencer_swp++; sequencer_swp &= 0x7;

				// clock the lengths
				if ((sequencer_len == 1) || (sequencer_len == 3) || (sequencer_len == 5) || (sequencer_len == 7))
				{
					if (SQ1_len_en && SQ1_len_cntr > 0)
					{
						SQ1_len_cntr--;
						if (SQ1_len_cntr == 0) { SQ1_enable = false; Audio_Regs[NR52] &= 0xFE; }
					}
					if (SQ2_len_en && SQ2_len_cntr > 0)
					{
						SQ2_len_cntr--;
						if (SQ2_len_cntr == 0) { SQ2_enable = false; Audio_Regs[NR52] &= 0xFD; }
					}
					if (WAVE_len_en && WAVE_len_cntr > 0)
					{
						WAVE_len_cntr--;
						if (WAVE_len_cntr == 0) { WAVE_enable = false; Audio_Regs[NR52] &= 0xFB; }
					}
					if (NOISE_len_en && NOISE_len_cntr > 0)
					{
						NOISE_len_cntr--;
						if (NOISE_len_cntr == 0) { NOISE_enable = false; Audio_Regs[NR52] &= 0xF7; }
					}
				}

				// clock the sweep
				if ((sequencer_swp == 3) || (sequencer_swp == 7))
				{
					SQ1_intl_swp_cnt++;
					if (SQ1_intl_swp_cnt >= SQ1_swp_prd)
					{
						SQ1_intl_swp_cnt = 0;

						if ((SQ1_swp_prd > 0) && (SQ1_shift > 0))
						{
							int shadow_frq = SQ1_frq_shadow;
							shadow_frq = shadow_frq >> SQ1_shift;
							if (SQ1_negate) { shadow_frq = -shadow_frq; }
							shadow_frq += SQ1_frq_shadow;

							// disable channel if overflow
							if ((uint)shadow_frq > 2047)
							{
								SQ1_enable = false;
								Audio_Regs[NR52] &= 0xFE;
							}
							else
							{
								shadow_frq &= 0x7FF;
								SQ1_frq = shadow_frq;
								SQ1_frq_shadow = shadow_frq;

								// note that we also write back the frequency to the actual register
								Audio_Regs[NR13] = (byte)(SQ1_frq & 0xFF);
								Audio_Regs[NR14] &= 0xF8;
								Audio_Regs[NR14] |= (byte)((SQ1_frq >> 8) & 7);

								// after writing, we repeat the process and do another overflow check
								shadow_frq = SQ1_frq_shadow;
								shadow_frq = shadow_frq >> SQ1_shift;
								if (SQ1_negate) { shadow_frq = -shadow_frq; }
								shadow_frq += SQ1_frq_shadow;

								if ((uint)shadow_frq > 2047)
								{
									SQ1_enable = false;
									Audio_Regs[NR52] &= 0xFE;
								}
							}
						}
					}
				}

				// clock the volume envelope
				if (sequencer_vol == 0)
				{
					if (SQ1_per > 0)
					{
						SQ1_vol_per++;
						if (SQ1_vol_per >= SQ1_per)
						{
							SQ1_vol_per = 0;
							if (SQ1_env_add && (SQ1_vol_state < 15)) { SQ1_vol_state++; }
							else if (SQ1_vol_state > 0) { SQ1_vol_state--; }
						}
					}
					if (SQ2_per > 0)
					{
						SQ2_vol_per++;
						if (SQ2_vol_per >= SQ2_per)
						{
							SQ2_vol_per = 0;
							if (SQ2_env_add && (SQ2_vol_state < 15)) { SQ2_vol_state++; }
							else if (SQ2_vol_state > 0) { SQ2_vol_state--; }
						}
					}
					if (NOISE_per > 0)
					{
						NOISE_vol_per++;
						if (NOISE_vol_per >= NOISE_per)
						{
							NOISE_vol_per = 0;
							if (NOISE_env_add && (NOISE_vol_state < 15)) { NOISE_vol_state++; }
							else if (NOISE_vol_state > 0) { NOISE_vol_state--; }
						}
					}
				}
			}
		}

		public void power_off()
		{
			for (int i = 0; i < 21; i++)
			{
				Audio_Regs[i] = 0;
			}
			SQ1_enable = false;
			SQ2_enable = false;
			WAVE_enable = false;
			NOISE_enable = false;

			sequencer_len = 0;
			sequencer_vol = 0;
			sequencer_swp = 0;
			sequencer_tick = 0;
			master_audio_clock = 0;
		}
		public void Reset()
		{
			Wave_RAM = new byte[16];

			Audio_Regs = new byte[21];

			AudioClocks = 0;
			master_audio_clock = 0;

			sequencer_len = 0;
			sequencer_swp = 0;
			sequencer_vol = 0;
			sequencer_tick = 0;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("Audio_Regs", ref Audio_Regs, false);
			ser.Sync("Wave_Ram", ref Wave_RAM, false);

			// save state variables
			ser.Sync("SQ1_length_counter", ref SQ1_len_cntr);
			ser.Sync("SQ2_length_counter", ref SQ2_len_cntr);
			ser.Sync("WAVE_length_counter", ref WAVE_len_cntr);
			ser.Sync("NOISE_length_counter", ref NOISE_len_cntr);
			ser.Sync("SQ1_enable", ref SQ1_enable);
			ser.Sync("SQ2_enable", ref SQ2_enable);
			ser.Sync("WAVE_enable", ref WAVE_enable);
			ser.Sync("NOISE_enable", ref NOISE_enable);
			ser.Sync("SQ1_vol_state", ref SQ1_vol_state);
			ser.Sync("SQ2_vol_state", ref SQ2_vol_state);
			ser.Sync("NOISE_vol_state", ref NOISE_vol_state);
			ser.Sync("SQ1_duty_cntr", ref SQ1_duty_cntr);
			ser.Sync("SQ2_duty_cntr", ref SQ2_duty_cntr);
			ser.Sync("WAVE_wave_cntr", ref WAVE_wave_cntr);
			ser.Sync("SQ1_frq_shadow", ref SQ1_frq_shadow);
			ser.Sync("SQ1_intl_cntr", ref SQ1_intl_cntr);
			ser.Sync("SQ2_intl_cntr", ref SQ2_intl_cntr);
			ser.Sync("WAVE_intl_cntr", ref WAVE_intl_cntr);
			ser.Sync("NOISE_intl_cntr", ref NOISE_intl_cntr);
			ser.Sync("SQ1_vol_per", ref SQ1_vol_per);
			ser.Sync("SQ2_vol_per", ref SQ2_vol_per);
			ser.Sync("NOISE_vol_per", ref NOISE_vol_per);
			ser.Sync("SQ1_intl_swp_cnt", ref SQ1_intl_swp_cnt);
			ser.Sync("NOISE_LFSR", ref NOISE_LFSR);
			ser.Sync("SQ1_len_cntr", ref SQ1_len_cntr);
			ser.Sync("SQ2_len_cntr", ref SQ2_len_cntr);
			ser.Sync("WAVE_len_cntr", ref WAVE_len_cntr);
			ser.Sync("NOISE_len_cntr", ref NOISE_len_cntr);


			ser.Sync("sequencer_len", ref sequencer_len);
			ser.Sync("sequencer_vol", ref sequencer_vol);
			ser.Sync("sequencer_swp", ref sequencer_swp);
			ser.Sync("sequencer_tick", ref sequencer_tick);

			ser.Sync("master_audio_clock", ref master_audio_clock);

			// get derived state
			if (ser.IsReader)
			{
				sync_channels();
			}
		}

		public void sync_channels()
		{

			SQ1_swp_prd = (byte)((Audio_Regs[NR10] & 0x70) >> 4);
			SQ1_negate = (Audio_Regs[NR10] & 8) > 0;
			SQ1_shift = (byte)(Audio_Regs[NR10] & 7);

			SQ1_duty = (byte)((Audio_Regs[NR11] & 0xC0) >> 6);
			SQ1_length = (ushort)(64 - Audio_Regs[NR11] & 0x3F);

			SQ1_st_vol = (byte)((Audio_Regs[NR12] & 0xF0) >> 4);
			SQ1_env_add = (Audio_Regs[NR12] & 8) > 0;
			SQ1_per = (byte)(Audio_Regs[NR12] & 7);

			SQ1_frq &= 0x700;
			SQ1_frq |= Audio_Regs[NR13];

			SQ1_trigger = (Audio_Regs[NR14] & 0x80) > 0;
			SQ1_len_en = (Audio_Regs[NR14] & 0x40) > 0;
			SQ1_frq &= 0xFF;
			SQ1_frq |= (ushort)((Audio_Regs[NR14] & 7) << 8);
		
			SQ2_duty = (byte)((Audio_Regs[NR21] & 0xC0) >> 6);
			SQ2_length = (ushort)(64 - Audio_Regs[NR21] & 0x3F);

			SQ2_st_vol = (byte)((Audio_Regs[NR22] & 0xF0) >> 4);
			SQ2_env_add = (Audio_Regs[NR22] & 8) > 0;
			SQ2_per = (byte)(Audio_Regs[NR22] & 7);

			SQ2_frq &= 0x700;
			SQ2_frq |= Audio_Regs[NR23];

			SQ2_trigger = (Audio_Regs[NR24] & 0x80) > 0;
			SQ2_len_en = (Audio_Regs[NR24] & 0x40) > 0;
			SQ2_frq &= 0xFF;
			SQ2_frq |= (ushort)((Audio_Regs[NR24] & 7) << 8);

			WAVE_DAC_pow = (Audio_Regs[NR30] & 0x80) > 0;

			WAVE_length = (ushort)(256 - Audio_Regs[NR31]);

			WAVE_vol_code = (byte)((Audio_Regs[NR32] & 0x60) >> 5);

			WAVE_frq &= 0x700;
			WAVE_frq |= Audio_Regs[NR33];

			WAVE_trigger = (Audio_Regs[NR34] & 0x80) > 0;
			WAVE_len_en = (Audio_Regs[NR34] & 0x40) > 0;
			WAVE_frq &= 0xFF;
			WAVE_frq |= (ushort)((Audio_Regs[NR34] & 7) << 8);

			NOISE_length = (ushort)(64 - Audio_Regs[NR41] & 0x3F);

			NOISE_st_vol = (byte)((Audio_Regs[NR42] & 0xF0) >> 4);
			NOISE_env_add = (Audio_Regs[NR42] & 8) > 0;
			NOISE_per = (byte)(Audio_Regs[NR42] & 7);

			NOISE_clk_shft = (byte)((Audio_Regs[NR43] & 0xF0) >> 4);
			NOISE_wdth_md = (Audio_Regs[NR43] & 8) > 0;
			NOISE_div_code = (byte)(Audio_Regs[NR43] & 7);

			WAVE_trigger = (Audio_Regs[NR44] & 0x80) > 0;
			WAVE_len_en = (Audio_Regs[NR44] & 0x40) > 0;

			AUD_CTRL_vin_L_en = (Audio_Regs[NR50] & 0x80) > 0;
			AUD_CTRL_vol_L = (byte)((Audio_Regs[NR50] & 0x70) >> 4);
			AUD_CTRL_vin_R_en = (Audio_Regs[NR50] & 8) > 0;
			AUD_CTRL_vol_R = (byte)(Audio_Regs[NR50] & 7);

			AUD_CTRL_noise_L_en = (Audio_Regs[NR51] & 0x80) > 0;
			AUD_CTRL_wave_L_en = (Audio_Regs[NR51] & 0x40) > 0;
			AUD_CTRL_sq2_L_en = (Audio_Regs[NR51] & 0x20) > 0;
			AUD_CTRL_sq1_L_en = (Audio_Regs[NR51] & 0x10) > 0;
			AUD_CTRL_noise_R_en = (Audio_Regs[NR51] & 8) > 0;
			AUD_CTRL_wave_R_en = (Audio_Regs[NR51] & 4) > 0;
			AUD_CTRL_sq2_R_en = (Audio_Regs[NR51] & 2) > 0;
			AUD_CTRL_sq1_R_en = (Audio_Regs[NR51] & 1) > 0;

			AUD_CTRL_power = (Audio_Regs[NR51] & 0x80) > 0;
		}

		#region audio

		public bool CanProvideAsync => false;

		public int AudioClocks;
		public short[] AudioSamples = new short[1500];

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported_");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = AudioClocks / 2;
			short[] temp_samp = new short[AudioClocks];

			for (int i = 0; i < AudioClocks; i++)
			{
				temp_samp[i] = AudioSamples[i];
			}

			samples = temp_samp;

			AudioClocks = 0;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			AudioClocks = 0;
		}

		private void GetSamples(short[] samples)
		{

		}

		#endregion
	}
}