﻿using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SkillCheck.Tests.Junior.IntegrationTests
{
    using SkillCheck.Structs;
    using SkillCheck.Tests.Senior.TestInfrastructure;

    [Collection("NoteTests")]
    public class NoteTests : IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public NoteTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost:52429")
            });
        }

        public async Task InitializeAsync()
        {
            // Асинхронная инициализация перед тестами, если требуется
        }

        public async Task DisposeAsync()
        {
            // Асинхронная очистка после тестов, если требуется
        }

        /*
         * Тесты:
         */

        [Fact]
        public async Task CreateEmptyNote_ReturnsCreatedNote()
        {
            var newNote = new
            {
                Title = "New Note",
                Content = ""
            };

            var httpContent = new StringContent(JsonSerializer.Serialize(newNote), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/notes/CreateNote", httpContent);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var createdNote = JsonSerializer.Deserialize<Note>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(createdNote);
            Assert.Equal("New Note", createdNote.Title);
            Assert.Equal("", createdNote.Content);
        }

        [Fact]
        public async Task CreateNoteFromTemplate_ReturnsCreatedNote()
        {
            var templateId = await CreateTestTemplateAsync();

            var httpContent = new StringContent(JsonSerializer.Serialize(templateId), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/notes/CreateNoteFromTemplate", httpContent);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var createdNote = JsonSerializer.Deserialize<Note>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(createdNote);
            Assert.Equal("Template Title", createdNote.Title);
        }

        [Fact]
        public async Task DeleteNote_RemovesNote()
        {
            var newNote = new
            {
                Title = "Note to Delete",
                Content = "Content"
            };

            var createContent = new StringContent(JsonSerializer.Serialize(newNote), Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/notes/CreateNote", createContent);
            createResponse.EnsureSuccessStatusCode();

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            var createdNote = JsonSerializer.Deserialize<Note>(createResponseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(createdNote);
            var noteId = createdNote.Id;

            var response = await _client.DeleteAsync($"/api/notes/DeleteNote/{noteId}");
            response.EnsureSuccessStatusCode();

            var getResponse = await _client.GetAsync($"/api/notes/GetNoteById/{noteId}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task EditNote_UpdatesNote()
        {
            var newNote = new
            {
                Title = "Original Title",
                Content = "Original Content"
            };

            var createContent = new StringContent(JsonSerializer.Serialize(newNote), Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/notes/CreateNote", createContent);
            createResponse.EnsureSuccessStatusCode();

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            var createdNote = JsonSerializer.Deserialize<Note>(createResponseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(createdNote);
            var noteId = createdNote.Id;

            var updatedNote = new
            {
                Title = "Updated Title",
                Content = "Updated Content"
            };

            var updateContent = new StringContent(JsonSerializer.Serialize(updatedNote), Encoding.UTF8, "application/json");

            var response = await _client.PutAsync($"/api/notes/EditNote/{noteId}", updateContent);
            response.EnsureSuccessStatusCode();

            var getResponse = await _client.GetAsync($"/api/notes/GetNoteById/{noteId}");
            getResponse.EnsureSuccessStatusCode();

            var getResponseContent = await getResponse.Content.ReadAsStringAsync();
            var note = JsonSerializer.Deserialize<Note>(getResponseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("Updated Title", note.Title);
            Assert.Equal("Updated Content", note.Content);
        }

        [Fact]
        public async Task SortNotesBySize_ReturnsSortedNotes()
        {
            var note1 = new
            {
                Title = "Small Note",
                Content = "Short"
            };

            var note2 = new
            {
                Title = "Large Note",
                Content = "This is a much longer content for the note."
            };

            var content1 = new StringContent(JsonSerializer.Serialize(note1), Encoding.UTF8, "application/json");
            var content2 = new StringContent(JsonSerializer.Serialize(note2), Encoding.UTF8, "application/json");

            var response1 = await _client.PostAsync("/api/notes/CreateNote", content1);
            response1.EnsureSuccessStatusCode();

            var response2 = await _client.PostAsync("/api/notes/CreateNote", content2);
            response2.EnsureSuccessStatusCode();

            // Получаем отсортированные заметки
            var response = await _client.GetAsync("/api/notes/SortNotesBySize");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var notes = JsonSerializer.Deserialize<List<Note>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.True(notes.Count >= 2, "There should be at least two notes.");

            Assert.True(notes[0].Content.Length <= notes[1].Content.Length);
        }

        private async Task<Guid> CreateTestTemplateAsync()
        {
            var newTemplate = new
            {
                Title = "Template Title",
                Content = "Template Content"
            };

            var templateContent = new StringContent(JsonSerializer.Serialize(newTemplate), Encoding.UTF8, "application/json");
            var templateResponse = await _client.PostAsync("/api/templates/CreateTemplate", templateContent);
            templateResponse.EnsureSuccessStatusCode();

            var templateResponseContent = await templateResponse.Content.ReadAsStringAsync();
            var createdTemplate = JsonSerializer.Deserialize<NoteTemplate>(templateResponseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return createdTemplate.Id;
        }

        /*
         * TODO:
         * Написать тесты, которые проверяют: 
         *  - получение списка блокнотов 
         *  - создание пустого блокнота или на основе шаблона
         *  - удаление блокнота
         *  - редактирование блокнота: заголовок, содержимое. 
         *    В содержимое блокнота можно указывать: текст, чекбокс с текстом, тексту можно задавать шрифт, жирность.
         *    Строчку можно перемещать по всему блокноту.
         *  - Создание шаблонного блокнота
         *  - Удаление шаблонного блокнота
         *  - Редактирование шаблонного блокнота
         *  - Сортировка блокнотов по размеру блокнота
         */
    }
}