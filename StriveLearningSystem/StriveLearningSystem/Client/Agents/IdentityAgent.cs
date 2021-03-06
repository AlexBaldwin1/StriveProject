﻿using Blazored.LocalStorage;
using Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using StriveLearningSystem.Client.Identity;
using StriveLearningSystem.Shared.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StriveLearningSystem.Client.Agents
{
    public class IdentityAgent
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ILocalStorageService _localStorage;

        public IdentityAgent(HttpClient httpClient,
                       AuthenticationStateProvider authenticationStateProvider,
                       ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
            _localStorage = localStorage;
        }

        public async Task<LoginResult> Login(Credential credential)
        {
            var loginAsJson = JsonSerializer.Serialize(credential);
            var response = await _httpClient.PostAsync("api/login", new StringContent(loginAsJson, Encoding.UTF8, "application/json"));
            var loginResult = JsonSerializer.Deserialize<LoginResult>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!response.IsSuccessStatusCode)
            {
                return loginResult;
            }

            await _localStorage.SetItemAsync("authToken", loginResult.Token);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", loginResult.Token);
            ((StriveAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(loginResult.Token);

            

            return loginResult;
        }

        public async Task Logout()
        {
            ((StriveAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut(); 
            await _localStorage.RemoveItemAsync("authToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<User> RegisterUser(User u)
        {
            try
            {
                var user = await _httpClient.PostJsonAsync<User>("api/register", u);
                return user;
            }
            catch(Exception e)
            {
                return null;
            }
        }

        public async Task<string> GetName()
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var username = authState.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Name).Value;

            return username;
        }

        public async Task<int> GetId()
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var userId = int.Parse(authState.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.NameIdentifier).Value);

            return userId;
        }



    }
}
